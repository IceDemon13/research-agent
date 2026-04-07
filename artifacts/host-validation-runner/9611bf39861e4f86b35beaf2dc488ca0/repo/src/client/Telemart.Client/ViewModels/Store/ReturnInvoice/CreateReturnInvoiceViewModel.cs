using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
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
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class CreateReturnInvoiceViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, ProductSourceDto[]> warehouseProductSources;

        public CreateReturnInvoiceViewModel(
           IWebClient webClient,
           IDictionaries dictionaries,
           IMessageFacadeService messageFacadeService,
           IMapper mapper,
           IMessenger messenger,
           ProductInformationViewModel productInformationViewModel)
           : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            ProductInformation = productInformationViewModel;

            SelectNpWarehouseCommand = new DelegateCommand(SelectNpWarehouse, () => ReceiverCityId != null && CarryType != null);
        }

        public CreateReturnInvoiceViewModel()
        {
        }

        public IDelegateCommand SelectNpWarehouseCommand { get; }

        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public DateTime? ReturnDate
        {
            get { return GetProperty(() => ReturnDate); }
            set { SetProperty(() => ReturnDate, value); }
        }

        public WarehouseDto Warehouse
        {
            get { return GetProperty(() => Warehouse); }
            set { SetProperty(() => Warehouse, value, WarehouseChanged); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value, ClearSelectedNpWarehouse); }
        }

        public NovaposhtaTtnPayer PayerType
        {
            get { return GetProperty(() => PayerType); }
            set { SetProperty(() => PayerType, value); }
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

        public string RecipientAddress
        {
            get { return GetProperty(() => RecipientAddress); }
            set { SetProperty(() => RecipientAddress, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            set { SetProperty(() => ProductInformation, value); }
        }

        public IEnumerable<SummaryViewItem> ReturnInvoiceItems
        {
            get { return GetProperty(() => ReturnInvoiceItems); }
            private set { SetProperty(() => ReturnInvoiceItems, value); }
        }

        public ReadOnlyObservableCollection<ReturnInvoiceProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<NovaposhtaTtnPayer> TtnPayers
        {
            get { return GetProperty(() => TtnPayers); }
            private set { SetProperty(() => TtnPayers, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReturnInvoiceProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, RefreshValues); }
        }

        private InvoiceDto Invoice
        {
            get { return GetProperty(() => Invoice); }
            set { SetProperty(() => Invoice, value); }
        }

        private ContractorDto Contractor
        {
            get { return GetProperty(() => Contractor); }
            set { SetProperty(() => Contractor, value); }
        }

        public bool IsEnableDeliveryInfo => CarryType == null || CarryType.Id == CarryType.PickupId;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateReturnInvoiceViewModel> builder)
        {
            builder.Property(x => x.Warehouse).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CarryType).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PayerType)
                .MatchesInstanceRule((x, y) => (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId) || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ReturnDate)
                .MatchesInstanceRule((x, y) => x?.Date >= y.Invoice?.DateArrive.Date, () => "Значение не может быть меньше даты прибытия накладной");
            builder.Property(x => x.ReceiverCityId)
                .MatchesInstanceRule((x, y) => x.HasValue || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.RecipientAddress)
                .MatchesInstanceRule((x, y) => !string.IsNullOrEmpty(x) || (y.CarryType?.Id != CarryType.NpDeliveryId && y.CarryType?.Id != CarryType.NpWarehouseId), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleOkAsync()
        {
            const string errorRu = "Ошибка при создании возврата";
            const string errorUkr = "Failed to create return invoice";

            try
            {
                ReturnInvoiceCreateDto returnInvoiceCreateDto = new ReturnInvoiceCreateDto()
                {
                    InvoiceId = InvoiceId,
                    WarehouseId = Warehouse.Id,
                    CarryId = CarryType.Id,
                    ReturnDate = (DateTime)ReturnDate,
                    TtnPayerTypeId = PayerType?.Id,
                    DeliveryData = DeliveryData,
                    ReceiverCityId = ReceiverCityId,
                    Products = Products.Where(x => x.OutQuantity > 0).Select(x => MapProduct(x, new ReturnInvoiceProductCreateDto())).ToList()
                };

                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CreateReturnInvoice(returnInvoiceCreateDto));

                MessageFacadeService.ShowNotificationInfo("Возврат успешно создан");

                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Added));

                Close();

                NonModalSizeableDialogDocumentManagerService.ShowView<ReturnInvoiceViewModel>(new ReturnInvoiceParameter(result.Data.Id), this);

                IsOk = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorUkr);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                Logger.LogError(exception, errorUkr);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            CreateReturnInvoiceParameter p = (CreateReturnInvoiceParameter)Parameter;

            Invoice = p.Invoice;

            InvoiceId = Invoice.Id;

            Contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(Invoice.SupplierId));

            Products = Invoice.InvoiceProducts
                .Where(x => x.QuantityReal - x.QuantityReturned > 0)
                .Select(x => Mapper.Map<ReturnInvoiceProductViewItem>(x))
                .ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>()
             .Where(x => x.Active)
             .OrderBy(x => x.Position)
             .ToReadOnlyObservableCollection();

            TtnPayers = new[] { NovaposhtaTtnPayer.Sender, NovaposhtaTtnPayer.Recipient }.ToReadOnlyObservableCollection();

            await Task.WhenAll(QueryCitiesAsync(), QueryWarehousesAsync());

            ProductSourceDto[] productSources = await WebClient.ExecuteApiRequestAsync(new QueryPurchaseSources(new QueryPurchaseSources.PurchaseSourcesRequest()
            {
                WarehouseIds = Warehouses.Select(x => x.Id).ToArray(),
                IncludeInvoices = false,
                IncludeTransits = false,
                StockStrategy = StockStrategy.Database,
                ProductIds = Products.Select(x => x.ProductId).ToArray()
            }));

            warehouseProductSources = productSources.GroupBy(x => x.WarehouseId).ToDictionary(x => x.Key, x => x.ToArray());

            ReturnInvoiceReportDataDto returnInvoiceReportDataDto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoiceSerialsPrintReport(InvoiceId));

            foreach (ReturnInvoiceProductViewItem product in Products)
            {
                product.SerialsQuantity = returnInvoiceReportDataDto.Products.Count(x => x.ProductId == product.ProductId);
            }

            ReturnInvoiceItems = GetSummaryItems();

            Title = "Создание возврата";
        }

        private static ReturnInvoiceProductCreateDto MapProduct(ReturnInvoiceProductViewItem source, ReturnInvoiceProductCreateDto target)
        {
            target.ProductId = source.ProductId;
            target.Quantity = source.OutQuantity;
            target.Price = source.Price;

            return target;
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
        }

        private async Task QueryWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && x.TypeId == WarehouseKind.Main.Id)
                .OrderByDescending(x => x.Position)
                .ToReadOnlyObservableCollection();
        }

        private void RefreshValues()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, SelectedProduct.CurrencyId);
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Номер накладной", Invoice.Id.ToString());
            yield return new SummaryViewItem("Поставщик", Contractor.Name);
            yield return new SummaryViewItem("Дата получения", Invoice.DateGet.ToString());
        }

        private void WarehouseChanged()
        {
            if (Warehouse is not null)
            {
                if (warehouseProductSources.TryGetValue(Warehouse.Id, out ProductSourceDto[] productSources))
                {
                    foreach (ReturnInvoiceProductViewItem product in Products)
                    {
                        ProductSourceDto source = productSources.FirstOrDefault(x => x.ProductId == product.ProductId);

                        if (source is null)
                        {
                            product.StockQuantity = 0;
                        }
                        else
                        {
                            product.StockQuantity = source.Quantity
                                                    - source.ReservedByOrder
                                                    - source.ReservedByShowcase
                                                    - source.ReservedByAssemblyComplectation
                                                    - source.ReservedByReturnInvoice;
                        }
                    }
                }
                else
                {
                    Products.ForEach(x => x.StockQuantity = 0);
                }
            }
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
    }
}