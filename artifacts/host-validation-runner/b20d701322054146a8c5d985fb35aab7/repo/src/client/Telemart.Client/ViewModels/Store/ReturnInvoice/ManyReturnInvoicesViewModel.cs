using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Invoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class ManyReturnInvoicesViewModel : TelemartDialogViewModelBase
    {
        private ManyReturnInvoiceParameter _manyReturnInvoiceParameter;

        public ManyReturnInvoicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;

            SelectNpWarehouseCommand = new DelegateCommand(SelectNpWarehouse, () => ReceiverCityId != null && CarryType != null);
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value, () => RaisePropertyChanged(nameof(ReturnPeriod))); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ContractorDto Contractor
        {
            get { return GetProperty(() => Contractor); }
            set { SetProperty(() => Contractor, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public DateTime? AcceptedFrom
        {
            get { return GetProperty(() => AcceptedFrom); }
            set { SetProperty(() => AcceptedFrom, value, () => RaisePropertiesChanged(nameof(AcceptedTo), nameof(ReturnDate))); }
        }

        public DateTime? AcceptedTo
        {
            get { return GetProperty(() => AcceptedTo); }
            set { SetProperty(() => AcceptedTo, value, () => RaisePropertiesChanged(nameof(AcceptedFrom), nameof(ReturnDate))); }
        }

        public DateTime? ReturnDate
        {
            get { return GetProperty(() => ReturnDate); }
            set { SetProperty(() => ReturnDate, value, () => RaisePropertiesChanged(nameof(AcceptedTo), nameof(AcceptedFrom))); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value, () => RaisePropertiesChanged(nameof(PayerType), nameof(ReceiverCityId), nameof(RecipientAddress))); }
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

        public int? ReceiverCityId
        {
            get { return GetProperty(() => ReceiverCityId); }
            set { SetProperty(() => ReceiverCityId, value); }
        }

        public string RecipientAddress
        {
            get { return GetProperty(() => RecipientAddress); }
            set { SetProperty(() => RecipientAddress, value); }
        }

        public DeliveryDataDto DeliveryData
        {
            get { return GetProperty(() => DeliveryData); }
            set { SetProperty(() => DeliveryData, value); }
        }

        public short ReturnPeriod => Contractor?.ReturnPeriod ?? 0;

        public IDelegateCommand SelectNpWarehouseCommand { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<ManyReturnInvoicesViewModel> builder)
        {
            builder.Property(x => x.WarehouseId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Contractor).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.AcceptedFrom)
                .MatchesInstanceRule((x, y) => x.HasValue && y.ReturnPeriod > 0 ? x >= DateTime.Now.AddDays(-1 * y.ReturnPeriod) && x <= y.AcceptedTo : x <= y.AcceptedTo, () => "Не может быть больше Даты до и не может превышать Период возврата у контрагента");
            builder.Property(x => x.AcceptedTo)
                .MatchesInstanceRule((x, _) => x.HasValue && x.Value <= DateTime.Now, () => "\"Принята до\" не может быть больше текущей даты");
            builder.Property(x => x.CarryType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PayerType)
                .MatchesInstanceRule((x, y) => (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId) || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ReturnDate)
                .MatchesInstanceRule((x, y) => !y.AcceptedTo.HasValue || x?.Date >= y.AcceptedTo.Value.Date, () => "Дата создания возврата не может быть меньше, чем дата принятия накладной");
            builder.Property(x => x.ReceiverCityId)
                .MatchesInstanceRule((x, y) => x.HasValue || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.RecipientAddress)
                .MatchesInstanceRule((x, y) => !string.IsNullOrEmpty(x) || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
        }

        public ManyReturnInvoiceParameter GetSeveralReturnInvoiceParameter()
        {
            return _manyReturnInvoiceParameter;
        }

        protected override async Task HandleLoadedAsync()
        {
            AcceptedTo = DateTime.Now;

            Title = "Создание возвратов";

            TtnPayers = new[] { NovaposhtaTtnPayer.Sender, NovaposhtaTtnPayer.Recipient }.ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            await Task.WhenAll(LoadContractorsAsync(), LoadWarehousesAsync(), LoadCitiesAsync());
        }

        protected override async Task HandleOkAsync()
        {
            Result<List<InvoiceDto>> resultInvoicesDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new Invoice1CSetInvoiceReturnedProducts(FilterForInvoices1C())),
                "запросе в 1С на количество товаров в накладной, которое можно вернуть",
                null,
                this,
                true,
                showNotification: false);

            if (resultInvoicesDtos.IsSuccess == false)
            {
                return;
            }

            if (resultInvoicesDtos.Data == null || resultInvoicesDtos.Data.Count == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Накладные с такими параметрами не найдены");

                return;
            }

            List<InvoiceDto> invoiceAfter1cDtos = resultInvoicesDtos.Data;

            InvoiceProductDto[] allInvoiceProductDtos = invoiceAfter1cDtos?.SelectMany(x => x.InvoiceProducts).ToArray();

            int productCountForReturn = allInvoiceProductDtos?.Count(x => x.QuantityReal - x.QuantityReturned > 0) ?? 0;

            if (productCountForReturn == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего возвращать");
                return;
            }

            _manyReturnInvoiceParameter = new ManyReturnInvoiceParameter(
                Contractor.Id,
                WarehouseId.Value,
                ReceiverCityId,
                ReturnDate.Value,
                CarryType.Id,
                PayerType?.Id,
                DeliveryData,
                invoiceAfter1cDtos);

            CloseOk();
        }

        private async Task LoadContractorsAsync()
        {
            PagedResult<ContractorDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryContractors(true)),
                "получении контрагентов",
                null,
                this,
                true,
                showNotification: false);

            Contractors = result.Data
                .Where(x => x.IsSupplier)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync(),
                "получении списка складов",
                null,
                this,
                true,
                showNotification: false);

            Warehouses = warehouses
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Main.Id)
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadCitiesAsync()
        {
            List<CityDto> cities = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync(),
                "получении городов",
                null,
                this,
                true,
                showNotification: false);

            Cities = cities
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private Invoice1CDto FilterForInvoices1C()
        {
            return new Invoice1CDto(
                new[] { WarehouseId.Value },
                new[] { Contractor.Id },
                AcceptedFrom,
                AcceptedTo,
                new[] { InvoiceState.Received.Id });
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
                    RecipientAddress = viewModel.FullAddressString;
                }
            }
            else if (CarryType.Kind == CarryTypeKind.Pickup)
            {
                SelectDeliveryWarehouseViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<SelectDeliveryWarehouseViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    DeliveryData = viewModel.GetDeliveryServiceData();
                    RecipientAddress = viewModel.Place.PlaceName;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}