using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class CreateServiceInvoiceViewModel : TelemartDialogViewModelBase
    {
        public CreateServiceInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public CreateServiceInvoiceViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ServiceCenterDto> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        public ServiceCenterDto SelectedServiceCenter
        {
            get { return GetProperty(() => SelectedServiceCenter); }
            set { SetProperty(() => SelectedServiceCenter, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public WarehouseDto SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            set { SetProperty(() => CarryTypes, value); }
        }

        public CarryType SelectedCarryType
        {
            get { return GetProperty(() => SelectedCarryType); }
            set { SetProperty(() => SelectedCarryType, value); }
        }

        public DateTime? SendDate
        {
            get { return GetProperty(() => SendDate); }
            set { SetProperty(() => SendDate, value); }
        }

        public bool IsServiceCenterReadOnly
        {
            get { return GetProperty(() => IsServiceCenterReadOnly); }
            private set { SetProperty(() => IsServiceCenterReadOnly, value); }
        }

        public bool IsWarehouseReadOnly
        {
            get { return GetProperty(() => IsWarehouseReadOnly); }
            private set { SetProperty(() => IsWarehouseReadOnly, value); }
        }

        #endregion INPC

        public ServiceInvoiceDto ResultServiceInvoice { get; private set; }

        public static void BuildMetadata(MetadataBuilder<CreateServiceInvoiceViewModel> builder)
        {
            builder.Property(x => x.SelectedServiceCenter).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedWarehouse).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCarryType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SendDate).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            CreateServiceInvoiceParameter parameter = (CreateServiceInvoiceParameter)Parameter;

            CarryTypes = Dictionaries.GetItems<CarryType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshServiceCenters(), RefreshWarehouses());

            if (parameter.ServiceCenterId.HasValue)
            {
                SelectedServiceCenter = ServiceCenters.FirstOrDefault(x => x.Id == parameter.ServiceCenterId);
                IsServiceCenterReadOnly = true;
            }

            if (parameter.WarehouseId.HasValue)
            {
                SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == parameter.WarehouseId);
                IsWarehouseReadOnly = true;
            }

            SendDate = DateTime.Today;

            Title = "Создание серв. накладной";

            async Task RefreshServiceCenters()
            {
                List<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true).GetPagedResultDataAsync();
                ServiceCenters = serviceCenters
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                Warehouses = warehouses
                    .Where(x => WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id) && x.Active == 1 && x.TypeId == WarehouseKind.Service.Id)
                    .OrderByDescending(x => x.Position)
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override async Task HandleOkAsync()
        {
            const string GeneralErrorMessage = "Ошибка при создании сервисной накладной";

            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            ServiceInvoiceCreateDto createDto = MapToDto(this, new ServiceInvoiceCreateDto());

            try
            {
                Result<ServiceInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CreateServiceInvoice(createDto));

                ResultServiceInvoice = result.Data;

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                ShowValidationResultView("Ошибки при создании сервисной заявки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create service invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                Logger.LogError(exception, "Error while creating service invoice");
            }
        }

        private static ServiceInvoiceCreateDto MapToDto(CreateServiceInvoiceViewModel source, ServiceInvoiceCreateDto target)
        {
            target.ServiceCenterId = source.SelectedServiceCenter.Id;
            target.WarehouseId = source.SelectedWarehouse.Id;
            target.CarryId = source.SelectedCarryType.Id;
            target.SendDate = source.SendDate.Value.Date;

            return target;
        }
    }
}