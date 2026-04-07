using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ChangeAddressReturnInvoiceViewModel : TelemartDialogViewModelBase
    {
        public ChangeAddressReturnInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;

            SelectNpWarehouseCommand = new DelegateCommand(SelectNpWarehouse, () => ReceiverCityId != null && CarryType != null);
        }

        public ChangeAddressReturnInvoiceViewModel()
        {
        }

        public DateTime? ReturnDate
        {
            get { return GetProperty(() => ReturnDate); }
            set { SetProperty(() => ReturnDate, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public WarehouseDto Warehouse
        {
            get { return GetProperty(() => Warehouse); }
            set { SetProperty(() => Warehouse, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value, ClearSelectedNpWarehouse); }
        }

        public ReadOnlyObservableCollection<NovaposhtaTtnPayer> TtnPayers
        {
            get { return GetProperty(() => TtnPayers); }
            private set { SetProperty(() => TtnPayers, value); }
        }

        public NovaposhtaTtnPayer PayerType
        {
            get { return GetProperty(() => PayerType); }
            set { SetProperty(() => PayerType, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public string RecipientAddress
        {
            get { return GetProperty(() => RecipientAddress); }
            set { SetProperty(() => RecipientAddress, value); }
        }

        public int? ReceiverCityId
        {
            get { return GetProperty(() => ReceiverCityId); }
            set { SetProperty(() => ReceiverCityId, value, ChangeReceiverCityId); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public DateTime InvoiceDateArrive
        {
            get { return GetProperty(() => InvoiceDateArrive); }
            set { SetProperty(() => InvoiceDateArrive, value); }
        }

        public ReturnInvoiceDto ReturnInvoice
        {
            get { return GetProperty(() => ReturnInvoice); }
            set { SetProperty(() => ReturnInvoice, value); }
        }

        public bool IsEnableDeliveryInfo => CarryType == null || CarryType.Id == CarryType.PickupId;

        public IDelegateCommand SelectNpWarehouseCommand { get; }

        public override int MaxHeight => 240;

        public override int MaxWidth => 460;

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<ChangeAddressReturnInvoiceViewModel> builder)
        {
            builder.Property(x => x.Warehouse).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PayerType)
                .MatchesInstanceRule((x, y) => (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId) || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ReturnDate).Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x >= y.InvoiceDateArrive, () => "Значение не может быть меньше даты прибытия накладной");
            builder.Property(x => x.ReceiverCityId)
                .MatchesInstanceRule((x, y) => x.HasValue || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.RecipientAddress)
                .MatchesInstanceRule((x, y) => !string.IsNullOrEmpty(x) || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ChangeAddressReturnInvoiceParameter parameter = (ChangeAddressReturnInvoiceParameter)Parameter;

            ReturnInvoiceDto returnInvoiceDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryReturnInvoice(parameter.InvoiceId)),
                "возвратной накладной",
                null,
                this,
                true,
                showDialog: false,
                showNotification: false);

            ReturnInvoice = returnInvoiceDto;

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            CarryType = CarryTypes.FirstOrDefault(x => x.Id == ReturnInvoice.CarryId);

            TtnPayers = new[] { NovaposhtaTtnPayer.Sender, NovaposhtaTtnPayer.Recipient }.ToReadOnlyObservableCollection();

            PayerType = TtnPayers.FirstOrDefault(x => x.Id == ReturnInvoice.TtnPayerTypeId);

            await Task.WhenAll(QueryWarehousesAsync(), QueryCitiesAsync(), QueryInvoiceAsync());

            Title = $"Изменение адреса возврата №{ReturnInvoice.Id}";
        }

        protected override async Task HandleOkAsync()
        {
            if (ReturnInvoice.ReturnDate == ReturnDate
                && ReturnInvoice.CarryId == CarryType.Id
                && ReturnInvoice.WarehouseId == Warehouse.Id
                && ReturnInvoice.ReceiverCityId == ReceiverCityId
                && NoChangeDeliveryData())
            {
                MessageFacadeService.ShowNotificationWarning("Нет изменений для сохранения");
                return;
            }

            ReturnInvoiceChangeAddressDto returnInvoiceChangeAddressDto = new ReturnInvoiceChangeAddressDto(ReturnDate.Value, Warehouse.Id, CarryType.Id, PayerType?.Id,  ReceiverCityId, DeliveryData);

            Result<ReturnInvoiceDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ChangeAddressReturnInvoice(ReturnInvoice.Id, returnInvoiceChangeAddressDto)),
                "адресса возвратной накладной",
                "Адрес сохранен",
                this,
                true);

            if (result != null)
            {
                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));

                CloseOk();
            }
        }

        private async Task QueryWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Main.Id)
                .OrderByDescending(x => x.Position)
                .ToReadOnlyObservableCollection();

            Warehouse = warehouses.FirstOrDefault(x => x.Id == ReturnInvoice.WarehouseId);
        }

        private async Task QueryCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Cities = cities
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ReceiverCityId = cities.FirstOrDefault(x => x.Id == ReturnInvoice.ReceiverCityId)?.Id;

            DeliveryData = ReturnInvoice.DeliveryData;

            RecipientAddress = DeliveryData?.Address;
        }

        private async Task QueryInvoiceAsync()
        {
            InvoiceDto invoiceDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryInvoice(ReturnInvoice.InvoiceId)),
                "возвратной накладной",
                null,
                this,
                true,
                showDialog: false,
                showNotification: false);

            InvoiceDateArrive = invoiceDto.DateArrive;

            ReturnDate = ReturnInvoice.ReturnDate;
        }

        private void SelectNpWarehouse()
        {
            if (ReceiverCityId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Сначала выберите город");

                return;
            }

            SelectDeliveryDataParameter parameter = new SelectDeliveryDataParameter(
                CarryType.Id,
                ReceiverCityId.Value,
                RecipientAddress,
                DeliveryData);

            if (CarryType.Kind == CarryTypeKind.Courier)
            {
                SelectDeliveryAddressViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                    parameter,
                    this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = DeliveryData.Address;
                }
            }
            else if (CarryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = DeliveryData.Address;
                }
            }
        }

        private void ClearSelectedNpWarehouse()
        {
            RecipientAddress = string.Empty;
            DeliveryData = null;
            PayerType = null;
            ReceiverCityId = null;

            RaisePropertiesChanged(nameof(PayerType), nameof(ReceiverCityId), nameof(RecipientAddress), nameof(IsEnableDeliveryInfo));
        }

        private void ChangeReceiverCityId()
        {
            RecipientAddress = string.Empty;
            DeliveryData = null;

            RaisePropertiesChanged(nameof(RecipientAddress));
        }

        private bool NoChangeDeliveryData()
        {
            if (DeliveryData != null)
            {
                return DeliveryData.Equals(ReturnInvoice.DeliveryData);
            }

            return ReturnInvoice.DeliveryData == null;
        }
    }
}