using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class EditInvoiceLogisticsViewModel : TelemartDialogViewModelBase
    {
        private int _invoiceId;

        public EditInvoiceLogisticsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public EditInvoiceLogisticsViewModel()
        {
        }

        #region INPC

        public int? SelectedEmployeeCarrierId
        {
            get { return GetProperty(() => SelectedEmployeeCarrierId); }
            set { SetProperty(() => SelectedEmployeeCarrierId, value); }
        }

        public int SelectedCarryId
        {
            get { return GetProperty(() => SelectedCarryId); }
            set { SetProperty(() => SelectedCarryId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public ObservableRangeCollection<InvoiceTtnViewItem> TrackNumbers
        {
            get { return GetProperty(() => TrackNumbers); }
            set { SetProperty(() => TrackNumbers, value); }
        }

        public ObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<CarryType> Carries
        {
            get { return GetProperty(() => Carries); }
            set { SetProperty(() => Carries, value); }
        }

        public bool NeedTrackNumber
        {
            get { return GetProperty(() => NeedTrackNumber); }
            private set { SetProperty(() => NeedTrackNumber, value, () => { RaisePropertyChanged(nameof(TrackNumbers)); }); }
        }

        public bool EditCarryEnabled
        {
            get { return GetProperty(() => EditCarryEnabled); }
            private set { SetProperty(() => EditCarryEnabled, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            const int MaxTtnCount = 10;

            _invoiceId = (int)Parameter;

            InvoiceDto invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(_invoiceId));
            CarryType invoiceCarryType = Dictionaries.GetItemById<CarryType>(invoice.CarryId);

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            WarehouseDto invoiceWarehouse = warehouses.First(x => x.Id == invoice.WarehouseId);
            CityDto invoiceWarehouseCity = cities.First(x => x.Id == invoiceWarehouse.CityId);

            Carries = Dictionaries.GetItems<CarryType>().ToObservableCollection();
            Employees = employees.Where(x => x.Active && x.CityId == invoiceWarehouseCity.Id).OrderBy(x => x.Name).ToObservableCollection();
            SelectedEmployeeCarrierId = invoice.EmployeeCarrierId;
            SelectedCarryId = invoice.CarryId;
            Comment = invoice.Comment;
            TrackNumbers = invoice.InvoiceTtns
                .Select(x => Map(x, new InvoiceTtnViewItem(invoiceCarryType)))
                .ToObservableRangeCollection();

            int deficitTtnCount = MaxTtnCount - TrackNumbers.Count;

            if (deficitTtnCount > 0)
            {
                TrackNumbers.AddRange(Enumerable.Range(1, deficitTtnCount).Select(x => new InvoiceTtnViewItem(invoiceCarryType) { Id = -x }));
            }

            NeedTrackNumber = !string.IsNullOrWhiteSpace(invoiceCarryType.TtnRegex);

            EditCarryEnabled = WebClient.IsOperationAllowed(BusinessOperation.InvoiceEditCarry) && invoice.StateId != InvoiceState.Cancelled.Id && invoice.StateId != InvoiceState.Received.Id;

            Title = $"Накладная №{_invoiceId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (TrackNumbers?.Any(x => IDataErrorInfoHelper.HasErrors(x)) == true)
            {
                return;
            }

            try
            {
                UpdateInvoiceLogistics gatewayRequest = new UpdateInvoiceLogistics(
                    _invoiceId,
                    SelectedEmployeeCarrierId,
                    SelectedCarryId,
                    Comment,
                    TrackNumbers.Where(x => !string.IsNullOrWhiteSpace(x.Ttn)).Select(x => Map(x, new InvoiceTtnDto())).ToArray());

                Result<InvoiceDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo("Логистика успешно обновлена");
                Messenger.Send(new InvoiceMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении логистики");
                ShowValidationResultView("Ошибки при обновлении логистики", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update invoice logistic");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while updating invoice logistic");
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении логистики");
            }
        }

        private InvoiceTtnViewItem Map(InvoiceTtnDto source, InvoiceTtnViewItem target)
        {
            target.Id = source.Id;
            target.Ttn = source.Ttn;

            return target;
        }

        private InvoiceTtnDto Map(InvoiceTtnViewItem source, InvoiceTtnDto target)
        {
            target.Id = source.Id;
            target.Ttn = source.Ttn;

            return target;
        }
    }
}