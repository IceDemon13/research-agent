using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Business.Delivery.TrackNumberProviders;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.PrintSticker;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.OrderBill;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.AdditionalServiceProduct;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    internal sealed class OrderPackViewModel : TelemartDialogViewModelBase
    {
        private const string SerialNumberScannedMessage = "Кол-во отсканированных SN не совпадает с заказом";

        private OrderPackInfoDto orderPackInfo;
        private WarehouseDto warehouse;

        private IReadOnlyDictionary<int, List<string>> accountingSystemSerials;
        private IReadOnlyDictionary<int, CityDto> cities;
        private IReadOnlyDictionary<int, ContractorDto> contractors;
        private ISet<string> _anotherOrdersNomenclatureSeries;

        private List<AssemblyServiceDto> assemblyServices;
        private IReadOnlyCollection<AdditionalServiceProductDto> _additionalServiceProducts;

        private decimal insurance;
        private OrderDto order;
        private Stopwatch stopwatch;
        private Subdivision subdivision;

        private ScanSerialMode scanSerialMode = ScanSerialMode.Single;

        private ITrackNumberProvider trackNumberProvider;

        public OrderPackViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMediator mediator,
            IOrderGiveHelper orderGiveHelper,
            IPrintingSettingsStore printingSettings,
            DocumentCommands documentCommands,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mediator = mediator;
            PrintingSettings = printingSettings;
            OrderGiveHelper = orderGiveHelper;
            DocumentCommands = documentCommands;
            ErrorHandler = errorHandler;
            Mapper = mapper;

            PrintWarrantyCardCommand = new AsyncCommand(PrintWarrantyCardAsync);
            PrintPackListCommand = new AsyncCommand<bool>(PrintPackListAsync);
            PrintAcceptanceProtocolCommand = new AsyncCommand(PrintAcceptanceProtocolAsync);
            HandlePreviewMouseLeftButtonUpCommand = new DelegateCommand<string>(HandlePreviewMouseLeftButtonUp);
            EditProductSerialsCommand = new DelegateCommand<OrderProductPackViewItem>(EditProductSerials, x => x != null);
            ClearScannedQuantityCommand = new DelegateCommand<OrderProductPackViewItem>(ClearScannedQuantity, x => x != null);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
            PrintBillCommand = new AsyncCommand(PrintBillAsync);
            PrintBillInvoiceCommand = new AsyncCommand(PrintBillInvoiceAsync);
            PackInternalCommand = new AsyncCommand<bool>(PackInternalAsync);
            FastGiveCommand = new AsyncCommand(FastGiveAsync, () => order != null && order.CarryId == CarryType.PickupId);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;
        }

        public OrderPackViewModel()
        {
        }

        #region Commands

        public IDelegateCommand EditProductSerialsCommand { get; }

        public IDelegateCommand ClearScannedQuantityCommand { get; }

        public IAsyncCommand FastGiveCommand { get; }

        public IDelegateCommand HandlePreviewMouseLeftButtonUpCommand { get; }

        public IDelegateCommand PrintAcceptanceProtocolCommand { get; }

        public IDelegateCommand PrintWarrantyCardCommand { get; }

        public IAsyncCommand PrintPackListCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand PrintBillCommand { get; }

        public IAsyncCommand PrintBillInvoiceCommand { get; }

        #endregion

        #region INPC

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            private set { SetProperty(() => Comment, value); }
        }

        public bool AllowBills
        {
            get { return GetProperty(() => AllowBills); }
            private set { SetProperty(() => AllowBills, value); }
        }

        public bool IsAutoPrintAcceptanceProtocol
        {
            get { return GetProperty(() => IsAutoPrintAcceptanceProtocol); }
            set { SetProperty(() => IsAutoPrintAcceptanceProtocol, value); }
        }

        public short AcceptanceProtocolQuantity
        {
            get { return GetProperty(() => AcceptanceProtocolQuantity); }
            set { SetProperty(() => AcceptanceProtocolQuantity, value); }
        }

        public bool IsAutoPrintCheque
        {
            get { return GetProperty(() => IsAutoPrintCheque); }
            set { SetProperty(() => IsAutoPrintCheque, value); }
        }

        public bool IsAutoPrintTrackNumber
        {
            get { return GetProperty(() => IsAutoPrintTrackNumber); }
            set { SetProperty(() => IsAutoPrintTrackNumber, value); }
        }

        public bool IsStickerPrintVisible
        {
            get { return GetProperty(() => IsStickerPrintVisible); }
            set { SetProperty(() => IsStickerPrintVisible, value); }
        }

        public bool IsGuestProductPrintVisible
        {
            get { return GetProperty(() => IsGuestProductPrintVisible); }
            set { SetProperty(() => IsGuestProductPrintVisible, value); }
        }

        public bool IsStickerFragile
        {
            get { return GetProperty(() => IsStickerFragile); }
            set { SetProperty(() => IsStickerFragile, value); }
        }

        public bool IsStickerThisWayUp
        {
            get { return GetProperty(() => IsStickerThisWayUp); }
            set { SetProperty(() => IsStickerThisWayUp, value); }
        }

        public bool IsAutoPrintWarrantyCard
        {
            get { return GetProperty(() => IsAutoPrintWarrantyCard); }
            set { SetProperty(() => IsAutoPrintWarrantyCard, value); }
        }

        public bool IsAutoPrintPackList
        {
            get { return GetProperty(() => IsAutoPrintPackList); }
            set { SetProperty(() => IsAutoPrintPackList, value); }
        }

        public bool IsAutoPrintBill
        {
            get { return GetProperty(() => IsAutoPrintBill); }
            set { SetProperty(() => IsAutoPrintBill, value); }
        }

        public bool IsAutoPrintBillInvoice
        {
            get { return GetProperty(() => IsAutoPrintBillInvoice); }
            set { SetProperty(() => IsAutoPrintBillInvoice, value); }
        }

        public bool IsAutoPrintBillInvoiceEnabled
        {
            get { return GetProperty(() => IsAutoPrintBillInvoiceEnabled); }
            private set { SetProperty(() => IsAutoPrintBillInvoiceEnabled, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public bool NeedToPrintAcceptanceProtocol
        {
            get { return GetProperty(() => NeedToPrintAcceptanceProtocol); }
            private set { SetProperty(() => NeedToPrintAcceptanceProtocol, value); }
        }

        public bool NeedToPrintPackList
        {
            get { return GetProperty(() => NeedToPrintPackList); }
            private set { SetProperty(() => NeedToPrintPackList, value); }
        }

        public bool NeedToPrintTrackNumber
        {
            get { return GetProperty(() => NeedToPrintTrackNumber); }
            private set { SetProperty(() => NeedToPrintTrackNumber, value); }
        }

        public bool IsGuestProductPrint
        {
            get { return GetProperty(() => IsGuestProductPrint); }
            set { SetProperty(() => IsGuestProductPrint, value); }
        }

        public OrderProductPackViewItem OrderProduct
        {
            get { return GetProperty(() => OrderProduct); }
            set { SetProperty(() => OrderProduct, value); }
        }

        public ObservableCollection<OrderProductPackViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            private set { SetProperty(() => OrderProducts, value); }
        }

        public string PrintChequeLabel
        {
            get { return GetProperty(() => PrintChequeLabel); }
            private set { SetProperty(() => PrintChequeLabel, value); }
        }

        public string ProgressText
        {
            get { return GetProperty(() => ProgressText); }
            private set { SetProperty(() => ProgressText, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 570;

        public override int MinHeight => 530;

        public override int MinWidth => 746;

        public override int Width => 900;

        #endregion

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public DocumentCommands DocumentCommands { get; }

        private IMediator Mediator { get; }

        private IMessenger Messenger { get; }

        private IOrderGiveHelper OrderGiveHelper { get; }

        private IPrintingSettingsStore PrintingSettings { get; }

        private IAsyncCommand PackInternalCommand { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            orderPackInfo = (OrderPackInfoDto)Parameter;

            accountingSystemSerials = orderPackInfo.ProductsAttributes.ToDictionary(x => x.ProductId, x => x.Serials);
            order = orderPackInfo.Order;
            subdivision = Dictionaries.GetItemById<Subdivision>(order.SubdivisionId);
            trackNumberProvider = Dictionaries.GetItemById<CarryType>(order.CarryId).GetTrackNumberProvider();
            insurance = orderPackInfo.Insurance;

            IFilteringItem filteringItem = new AssemblyServicesFilteringItem
            {
                OrderIds = orderPackInfo.Order.Id.ToString()
            };

            IFilteringItem additionalServiceProductsFilteringItem = new AdditionalServiceProductsFilteringItem()
            {
                OrderIds = orderPackInfo.Order.Id.ToString(),
                ControlInMovements = true
            };

            (WarehouseDto warehouse, List<AssemblyServiceDto> assemblyServices, PagedResult<AdditionalServiceProductDto>
                additionalServiceProducts, PagedResult<ContractorDto> contractors, PagedResult<CityDto> cities, Result _) result =
                    await TaskExt.WhenAll(
                        WebClient.ExecuteApiRequestAsync(new QueryWarehouse(order.WarehouseId!.Value)),
                        WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filteringItem)).GetPagedResultDataAsync(),
                        WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(additionalServiceProductsFilteringItem)),
                        WebClient.ExecuteApiRequestAsync(new QueryContractors(), true),
                        WebClient.ExecuteApiRequestAsync(new QueryCities(), true),
                        FetchAssemblyServicesAsync(orderPackInfo.Order.Id));

            warehouse = result.warehouse;
            assemblyServices = result.assemblyServices;
            contractors = result.contractors.Data.ToDictionary(x => x.Id);
            cities = result.cities.Data.ToDictionary(x => x.Id);
            _additionalServiceProducts = result.additionalServiceProducts?.Data.ToArray() ?? Array.Empty<AdditionalServiceProductDto>();

            RecognizeBarcodeViewModel.Init(
                new RecognizeBarcodeSettings(
                true,
                true,
                allowOurAssemblyService: true,
                additionalServiceProductIds: _additionalServiceProducts.Select(x => x.Id).ToArray()),
                orderPackInfo.ProductsAttributes);

            IReadOnlyCollection<OrderProductPackViewItem> packViewItems = GetOrderProductPackViewItems(order, _additionalServiceProducts).ToArray();

            OrderProducts = packViewItems.OrderBy(x => x.ProductName).ToObservableCollection();

            Comment = OrderCommentHelper.GetJoinedComment(order.CustomerComment, order.EmployeeComment, order.SystemComment);

            SummaryItems = GetSummaryItems();

            PrintChequeLabel = subdivision.IsRetail
                ? "Чек"
                : "Накладная";

            if (warehouse.UseCells)
            {
                bool isNovaPoshta = CarryType.IsNovaposhta(order.CarryId);

                IsAutoPrintCheque = false;
                IsAutoPrintTrackNumber = isNovaPoshta;
                IsAutoPrintBillInvoice = isNovaPoshta;
                IsAutoPrintBill = isNovaPoshta;
                IsAutoPrintWarrantyCard = order.CarryId != CarryType.PickupId && subdivision.IsRetail;
            }
            else
            {
                IsAutoPrintBill = true;
                IsAutoPrintBillInvoice = true;
                IsAutoPrintCheque = false;
                IsAutoPrintWarrantyCard = subdivision.IsRetail;
                IsAutoPrintTrackNumber = true;
            }

            IsAutoPrintBillInvoiceEnabled = !IsAutoPrintBillInvoice;

            bool hasProductInServiceRequest = await HasProductInServiceRequestAsync();

            IsAutoPrintAcceptanceProtocol = hasProductInServiceRequest == false && order.CarryId != CarryType.PickupId;

            NeedToPrintAcceptanceProtocol = true;
            NeedToPrintPackList = true;
            AcceptanceProtocolQuantity = order.PaymentId == Payment.PaylaterId ? (short)2 : (short)1;
            NeedToPrintTrackNumber = trackNumberProvider.SupportTrackNumbers;

            if (orderPackInfo.BillId.HasValue)
            {
                AllowBills = true;

                IsAutoPrintCheque = false;
                IsAutoPrintAcceptanceProtocol = false;
            }

            CarryType carryType = Dictionaries.GetItemById<CarryType>(order.CarryId);

            if (carryType.StickerRequired)
            {
                IsStickerPrintVisible = true;
                IsStickerFragile = orderPackInfo.ProductsAttributes.Any(x => x.StickerFragile.HasValue && x.StickerFragile.Value);
                IsStickerThisWayUp = orderPackInfo.ProductsAttributes.Any(x => x.StickerThisWayUp.HasValue && x.StickerThisWayUp.Value);
            }
            else
            {
                IsStickerPrintVisible = IsStickerFragile = IsStickerThisWayUp = false;
            }

            Title = $"Упаковка заказа №{order.Id.ToString(CultureInfo.InvariantCulture)}";

            stopwatch = Stopwatch.StartNew();

            if (!string.IsNullOrEmpty(orderPackInfo.ScannedBarcode))
            {
                RecognizeBarcodeViewModel.RecognizeBarcodeCommand.Execute(orderPackInfo.ScannedBarcode);
            }

            List<OrderDocumentDto> orderDocuments = await WebClient.ExecuteApiRequestAsync(new QueryDocumentsByOrders(new[] { order.Id }, (int)OrderDocumentTypeIds.ActIncomeId));

            IsGuestProductPrintVisible = IsGuestProductPrint = _additionalServiceProducts?.Any(x => !string.IsNullOrEmpty(x.GuestProduct?.Product)
                                                                   || x.ConsumableProducts?.Any(y => !string.IsNullOrEmpty(y.GuestProduct?.Product)) == true) == true
                                                               && order.CarryId != CarryType.PickupId && orderDocuments?.Any() == true;
        }

        protected override async Task HandleOkAsync()
        {
            await PackInternalAsync(true);
        }

        protected override void OnInitializeInDesignMode()
        {
            SummaryItems = new[]
            {
                new SummaryViewItem("Подразделение", "Телемарт"),
                new SummaryViewItem("Контрагент", "!Телемарт"),
                new SummaryViewItem("Доставка", "Курьер")
            };

            PrintChequeLabel = "Чек";

            NeedToPrintAcceptanceProtocol = true;
            NeedToPrintPackList = true;
            NeedToPrintTrackNumber = true;

            IsAutoPrintCheque = true;
            IsAutoPrintAcceptanceProtocol = true;
            IsAutoPrintWarrantyCard = true;
            IsAutoPrintPackList = false;
            IsAutoPrintTrackNumber = true;

            AllowBills = true;
        }

        private static Task<bool> IsPrintingSettingsValidAsync(PrintingSettingsInfo settings, CancellationToken cancellationToken)
        {
            if (settings?.Main?.Name == null ||
                settings.WarrantyCard?.Name == null ||
                (settings.ChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id && settings.Cheque?.Name == null))
            {
                return Task.FromResult(false);
            }

            return Task<bool>.Factory.StartNew(IsPrintingSettingsValid, cancellationToken);

            bool IsPrintingSettingsValid()
            {
                List<string> printers = new List<string> { settings.Main.Name, settings.WarrantyCard.Name };

                if (settings.ChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id)
                {
                    printers.Add(settings.Cheque.Name);
                }

                return printers
                    .Select(printerName => new PrinterSettings { PrinterName = printerName })
                    .All(printerSettings => printerSettings.IsValid);
            }
        }

        private async Task<bool> PackInternalAsync(bool useCells)
        {
            if (AcceptanceProtocolQuantity < 0)
            {
                MessageFacadeService.ShowNotificationError("Кол-во копий не должно быть отрицательным");
                return false;
            }

            ProgressText = "Обработка данных";

            try
            {
                ValidationResultItem[] warnings = OrderProducts
                    .Where(x => x.KeepSerial && x.Serials.Any(y => !accountingSystemSerials[x.ProductId].Contains(y)))
                    .Select(x => $"{x.ProductName}. Нет SN в 1С: {string.Join(", ", x.Serials.Where(y => !accountingSystemSerials[x.ProductId].Contains(y)))}")
                    .Select(x => new ValidationResultItem(x, false))
                    .ToArray();

                if (warnings.Any() && !ShowValidationResultView("Предупреждения", warnings))
                {
                    return false;
                }

                List<ProductComparisonResult> items = OrderProducts
                    .Where(x => x.HasDeviation)
                    .Select(x => new ProductComparisonResult(x.ProductName, x.Quantity, (int)x.ScannedQuantity))
                    .ToList();

                if (items.Any())
                {
                    bool allowContinue = OrderProducts.All(x => !x.HasDeviation && (!x.KeepSerial || x.Serials?.Count > 0));

                    ProductComparisonResultViewModel validationViewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(
                        new object[] { items, allowContinue },
                        this);

                    if (!validationViewModel.IsOk)
                    {
                        return false;
                    }
                }

                if (IsAutoPrintCheque || IsAutoPrintAcceptanceProtocol || IsAutoPrintWarrantyCard || IsAutoPrintTrackNumber || IsAutoPrintPackList)
                {
                    PrintingSettingsInfo settings = await PrintingSettings.LoadAsync();

                    bool isPrintingSettingsValid;

                    string progressText = ProgressText;

                    ProgressText = "Инициализация принтеров";

                    using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        isPrintingSettingsValid = await IsPrintingSettingsValidAsync(settings, cancellationTokenSource.Token);
                    }

                    ProgressText = progressText;

                    if (!isPrintingSettingsValid)
                    {
                        MessageFacadeService.ShowNotificationError("Задайте принтеры в настройках");
                        return false;
                    }
                }

                stopwatch.Stop();

                int[] cellIds = orderPackInfo.CellIds;

                if (order.CarryId == CarryType.PickupId)
                {
                    if (warehouse.UseCells && useCells)
                    {
                        OrderPackCellViewModel orderPackCellViewModel = SizeableDialogDocumentManagerService.ShowView<OrderPackCellViewModel>(new OrderPackCellParameter(cellIds, order.WarehouseId!.Value), this);

                        if (orderPackCellViewModel.IsOk)
                        {
                            cellIds = orderPackCellViewModel.WarehouseCells.Select(x => x.Id).ToArray();
                        }
                        else
                        {
                            return false;
                        }

                        if (cellIds.Any() != true)
                        {
                            if (!MessageFacadeService.Confirm("Вы уверены, что хотите упаковать заказ без использования ячеек?"))
                            {
                                return false;
                            }
                        }
                    }
                }

                PackageProperties packageProperties = GetPackageProperties(order);

                if (packageProperties == null)
                {
                    return false;
                }

                string packageTtn = string.Empty;

                if (trackNumberProvider.SupportTrackNumbers)
                {
                    packageTtn = await trackNumberProvider.CreateAsync(order.Id, packageProperties);
                }

                OrderPackDto orderDeliverDto = GetOrderDeliveryDto(stopwatch.Elapsed, packageProperties.PlaceCount, packageProperties.TotalWeight, packageTtn, cellIds);

                OrderDto packResult = await TryPackOrderAsync(orderDeliverDto);

                if (packResult != null)
                {
                    await PrintDocumentsAsync(packResult);

                    IsOk = true;
                    Close();
                }
            }
            catch (OperationCanceledException exception)
            {
                Logger.LogError(exception, "Printer settings validation timed out");
                MessageFacadeService.ShowNotificationError("Ошибка инициализации принтеров");

                return false;
            }
            catch (CreateTrackNumberException exception)
            {
                ShowValidationResultView(
                    "Ошибки при создании ТТН",
                    exception.Args.ErrorMessages.Select(x => new ValidationResultItem(x, true)).ToArray());

                return false;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to deliver order");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке заказа");

                return false;
            }

            return true;
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Подразделение", Dictionaries.GetItemById<Subdivision>(order.SubdivisionId).Name);
            yield return new SummaryViewItem("Контрагент", contractors.GetValueOrDefault(order.ClientId)?.Name);
            yield return new SummaryViewItem("Доставка", Dictionaries.GetItemById<CarryType>(order.CarryId).Name);
        }

        private RecognizeBarcodeMessage AddProduct(ProductAttributesDto product, int quantity, int? additionalServiceProductId = null)
        {
            OrderProductPackViewItem packViewItem;

            if (additionalServiceProductId.HasValue)
            {
                packViewItem = OrderProducts.FirstOrDefault(x => x.AdditionalServiceProductId == additionalServiceProductId.Value);

                if (packViewItem == null)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Такой услуги нет в заказе");
                }

                if (packViewItem.ScannedQuantity > 0)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Услуга уже просканирована");
                }

                if (packViewItem.KeepSerialOverridden)
                {
                    string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(packViewItem.ProductId).ToArray();

                    ProductSerialsViewModelParameter parameter = new(
                        packViewItem.ProductId,
                        packViewItem.Serials.ToList(),
                        barcodes,
                        null,
                        scanSerialMode,
                        accountingSystemSerials[packViewItem.ProductId],
                        scanExistingSerials: true);

                    ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                    if (viewModel.IsOk)
                    {
                        string firstSerialNumber = viewModel.SerialNumbers.First();

                        if (firstSerialNumber != packViewItem.Serials.First())
                        {
                            MessageFacadeService.ShowMessageBoxWarning("Для сканирования товара с оказанной услугой необходимо повторно отсканировать ШК услуги");

                            return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "SN не соответствует товару, на который оказывалась услуга");
                        }

                        scanSerialMode = viewModel.ScanMode;

                        packViewItem.ScannedQuantity = 1;
                    }
                }
                else
                {
                    packViewItem.ScannedQuantity = 1;
                }
            }
            else
            {
                packViewItem = OrderProducts
                    .OrderByDescending(x => x.ScannedQuantity < x.Quantity)
                    .FirstOrDefault(x => x.ProductId == product.ProductId && x.AssemblyId == null && x.AdditionalServiceProductId == null);

                if (packViewItem is null)
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товара {product.FullName} нет в заказе");
                }

                if (packViewItem.KeepSerialOverridden)
                {
                    string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(packViewItem.ProductId).ToArray();

                    if (string.IsNullOrEmpty(packViewItem.NomenclatureSeries))
                    {
                        ProductSerialsViewModelParameter parameter = new(
                            packViewItem.ProductId,
                            packViewItem.Serials.ToList(),
                            barcodes,
                            product.SerialNumberLength,
                            scanSerialMode,
                            accountingSystemSerials[packViewItem.ProductId]);

                        ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                        if (viewModel.IsOk)
                        {
                            if (packViewItem.ProductTypeId == ProductType.AssembledComputerRuleId
                                && viewModel.SerialNumbers.Any(x => _anotherOrdersNomenclatureSeries.Contains(x)))
                            {
                                const string anotherOrderAssemblyErrorMessage = "Конфигурация ПК относится к другому заказу";

                                if (warehouse.TreatErrorsAsWarnings)
                                {
                                    MessageFacadeService.ShowMessageBoxWarning(anotherOrderAssemblyErrorMessage);
                                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, anotherOrderAssemblyErrorMessage);
                                }

                                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, anotherOrderAssemblyErrorMessage);
                            }

                            packViewItem.Serials.AddRange(viewModel.SerialNumbers);
                            packViewItem.ScannedQuantity = packViewItem.Serials.Count;

                            scanSerialMode = viewModel.ScanMode;
                        }
                    }
                    else
                    {
                        string[] allowSerialsByProductId = OrderProducts
                            .OrderByDescending(x => x.ScannedQuantity < x.Quantity)
                            .Where(x => x.ScannedQuantity < x.Quantity && x.ProductId == product.ProductId && x.AssemblyId == null && x.AdditionalServiceProductId == null)
                            .Select(x => x.NomenclatureSeries).ToArray();

                        ProductSerialsViewModelParameter parameter = new(
                            packViewItem.ProductId,
                            product.NomenclatureSeriesAccounting ? allowSerialsByProductId : Array.Empty<string>(),
                            barcodes,
                            product.SerialNumberLength,
                            scanSerialMode,
                            accountingSystemSerials[packViewItem.ProductId],
                            scanExistingSerials: product.NomenclatureSeriesAccounting,
                            title: "SN для конфигурации ПК",
                            existingSerialsError: "SN не соответствует конфигурации ПК");

                        ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                        if (viewModel.IsOk)
                        {
                            if (viewModel.SerialNumbers.Count != packViewItem.Serials.Count)
                            {
                                MessageFacadeService.ShowNotificationError("Кол-во SN не соответствует кол-ву\n конфигураций ПК");
                                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Кол-во SN не соответствует кол-ву\n конфигураций ПК");
                            }

                            if (product.NomenclatureSeriesAccounting && viewModel.SerialNumbers.Any(x => !allowSerialsByProductId.Contains(x)))
                            {
                                MessageFacadeService.ShowNotificationError("Просканированы несуществующие SN");
                                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Просканированы несуществующие SN");
                            }

                            packViewItem = OrderProducts.FirstOrDefault(x =>
                                x.ProductId == product.ProductId
                                && x.AssemblyId == null
                                && x.AdditionalServiceProductId == null
                                && viewModel.SerialNumbers.Contains(x.NomenclatureSeries));

                            if (packViewItem != null && packViewItem.ScannedQuantity >= packViewItem.Quantity)
                            {
                                MessageFacadeService.ShowNotificationError("SN уже просканирован для другого товара");
                                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "SN уже просканирован для другого товара");
                            }

                            scanSerialMode = viewModel.ScanMode;

                            packViewItem = OrderProducts.FirstOrDefault(
                                x =>
                                    x.ProductId == product.ProductId
                                    && x.AssemblyId == null
                                    && x.AdditionalServiceProductId == null);

                            if (packViewItem is not null)
                            {
                                packViewItem.ScannedQuantity = packViewItem.Serials.Count;
                            }
                            else
                            {
                                MessageFacadeService.ShowNotificationError("Не удалось найти конфигурацию ПК");
                                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Не удалось найти конфигурацию ПК");
                            }
                        }
                    }
                }
                else
                {
                    packViewItem.ScannedQuantity += quantity;
                }
            }

            OrderProduct = packViewItem;

            if (OrderProducts.All(x => x.Quantity == x.ScannedQuantity)
                && warehouse.UseCells
                && order.CarryId == CarryType.PickupId)
            {
                PackInternalCommand.Execute(true);
            }

            return null;
        }

        private void ClearScannedQuantity(OrderProductPackViewItem productPackViewItem)
        {
            productPackViewItem.ScannedQuantity = 0;
            productPackViewItem.Serials.Clear();
        }

        private void EditProductSerials(OrderProductPackViewItem productPackViewItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(productPackViewItem.Serials.ToList(), productPackViewItem.AdditionalServiceProductId.HasValue), this);

            if (viewModel.IsOk)
            {
                if (productPackViewItem.Serials.Count != viewModel.SerialNumbers.Count)
                {
                    productPackViewItem.ScannedQuantity = viewModel.SerialNumbers.Count;
                }

                productPackViewItem.Serials = new ObservableRangeCollection<string>(viewModel.SerialNumbers);
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            if (string.Equals(e.FieldName, nameof(OrderProductPackViewItem.ScannedQuantity), StringComparison.Ordinal))
            {
                OrderProductPackViewItem productPackViewItem = (OrderProductPackViewItem)e.Data;

                if (productPackViewItem.KeepSerialOverridden)
                {
                    EditProductSerialsCommand.Execute(productPackViewItem);
                }
            }
        }

        private async Task PrintBillInvoiceAsync()
        {
            try
            {
                if (orderPackInfo.BillId.HasValue)
                {
                    byte[] document = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBillInvoice(orderPackInfo.BillId.Value));
                    await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
                }
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати расходной накладной");
                Logger.LogError(ex, "Failed to print bill invoice");
            }
        }

        private async Task FastGiveAsync()
        {
            bool success = await PackInternalAsync(false);

            if (!success)
            {
                return;
            }

            CityDto city = cities.GetValueOrDefault(order.CityId ?? -1);

            ContractorDto contractor = contractors.GetValueOrDefault(order.ClientId);

            await WebClient.ExecuteApiRequestAsync(new UnlockOrder(order.Id));

            await OrderGiveHelper.FastGiveAsync(order.Id, contractor?.Name, city?.Name, this);
        }

        private async Task PrintBillAsync()
        {
            try
            {
                if (orderPackInfo.BillId.HasValue)
                {
                    byte[] document = await WebClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBill(orderPackInfo.BillId.Value));
                    await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
                }
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати счета");
                Logger.LogError(ex, "Failed to print bill");
            }
        }

        private OrderPackDto GetOrderDeliveryDto(TimeSpan elapsed, int packagePlaces, decimal packageWeight, string packageTtn, int[] cellIds)
        {
            return new OrderPackDto(
                order.Id,
                packagePlaces,
                packageWeight,
                packageTtn,
                OrderProducts
                    .SelectMany(
                    x =>
                    {
                        Queue<string> serialNumbers = new(x.Serials);

                        return x.OrderProducts.Select(y => new OrderProductDeliveryDto(y.OrderProductId, serialNumbers.TryDequeueChank(y.Quantity).ToArray()));
                    })
                    .GroupBy(x => x.OrderProductId)
                    .Select(x => new OrderProductDeliveryDto(x.Key, x.SelectMany(y => y.Serials).ToArray()))
                    .ToArray(),
                elapsed,
                cellIds);
        }

        private IEnumerable<OrderProductPackViewItem> GetOrderProductPackViewItems(
            OrderDto orderObj,
            IReadOnlyCollection<AdditionalServiceProductDto> additionalServiceProducts)
        {
            IReadOnlyCollection<AssemblyServiceProductDto> assemblyServiceProducts = assemblyServices.SelectMany(x => x.Products).ToArray();

            (int orderProductId, int? parentRecordId)[] allOrderProductIdsWithParent = orderObj.Products.Select(x => (x.Id, x.ParentRecordId)).ToArray();

            foreach (IGrouping<int, OrderProductDto> orderProductGroup in orderObj.Products.GroupBy(x => x.Product.Id))
            {
                int quantity = orderProductGroup.Sum(x => x.Quantity);
                int[] orderProductIds = orderProductGroup.Select(x => x.Id).ToArray();
                int warrantyId = orderProductGroup.First().WarrantyId;

                string[] serialNumbers = orderProductGroup.Where(x => x.SerialNumbers != null).SelectMany(x => x.SerialNumbers.Select(z => z.SerialNumber)).ToArray();

                ProductAttributesDto product = RecognizeBarcodeViewModel.FindById(orderProductGroup.Key);

                List<string> usedSerialNumbers = new List<string>();

                foreach (OrderProductDto consumableOrderProduct in orderProductGroup.Where(x => x.IsAdditionalServiceConsumable))
                {
                    OrderProductPackViewItem consumableOrderProductItem = new OrderProductPackViewItem
                    {
                        IsConsumableAdditionalServiceProduct = true,
                        Id = consumableOrderProduct.Id,
                        ProductId = product.ProductId,
                        Name = product.FullName,
                        NameUkr = product.FullNameUa,
                        NameEn = product.FullNameEn,
                        ProductTypeId = product.TypeId,
                        ProductFullNameUa = product.FullNameUa,
                        PrintWarrantyCard = product.PrintWarrantyCard,
                        KeepSerial = product.KeepSerial,
                        KeepSerialOverridden = product.KeepSerial,
                        Quantity = consumableOrderProduct.Quantity,
                        ScannedQuantity = consumableOrderProduct.Quantity,
                        WarrantyId = warrantyId,
                        OrderProducts = consumableOrderProduct.Yield()
                            .Select(x => new OrderProductQuantityViewItem { OrderProductId = x.Id, Quantity = x.Quantity })
                            .ToArray()
                    };

                    if (serialNumbers.Any())
                    {
                        string[] consumableProductsSerialNumbers = consumableOrderProduct.SerialNumbers
                            .Select(x => x.SerialNumber)
                            .Where(x => !usedSerialNumbers.Contains(x))
                            .ToArray();

                        consumableOrderProductItem.Serials.AddRange(consumableProductsSerialNumbers);
                        usedSerialNumbers.AddRange(consumableProductsSerialNumbers);
                    }

                    consumableOrderProductItem.GroupString = "Товары для оказания услуг";

                    quantity -= consumableOrderProduct.Quantity;

                    yield return consumableOrderProductItem;
                }

                foreach (AssemblyServiceProductDto assemblyServiceProduct in assemblyServiceProducts.Where(x => x.OrderProductId.HasValue && orderProductIds.Contains(x.OrderProductId ?? 0)))
                {
                    if (quantity == 0)
                    {
                        break;
                    }

                    OrderProductPackViewItem item = new OrderProductPackViewItem
                    {
                        Id = assemblyServiceProduct.OrderProductId!.Value,
                        ProductId = product.ProductId,
                        Name = product.FullName,
                        NameUkr = product.FullNameUa,
                        NameEn = product.FullNameEn,
                        ProductTypeId = product.TypeId,
                        ProductFullNameUa = product.FullNameUa,
                        PrintWarrantyCard = product.PrintWarrantyCard,
                        KeepSerial = product.KeepSerial,
                        KeepSerialOverridden = product.KeepSerial,
                        Quantity = 0,
                        WarrantyId = warrantyId,
                        OrderProducts = new[]
                        {
                            new OrderProductQuantityViewItem
                            {
                                OrderProductId = assemblyServiceProduct.OrderProductId.Value,
                                Quantity = orderProductGroup.First(x => x.Id == assemblyServiceProduct.OrderProductId!.Value).Quantity
                            }
                        }
                    };

                    if (assemblyServiceProduct.SerialNumbers?.Any() == true)
                    {
                        item.Serials.AddRange(assemblyServiceProduct.SerialNumbers);
                        usedSerialNumbers.AddRange(assemblyServiceProduct.SerialNumbers);
                    }

                    item.Quantity = assemblyServiceProduct.Quantity;
                    item.AssemblyId = assemblyServiceProduct.AssemblyServiceId;
                    item.GroupString = $"Сборка: {assemblyServiceProduct.AssemblyServiceId}";

                    quantity -= assemblyServiceProduct.Quantity;

                    yield return item;
                }

                foreach (AdditionalServiceProductDto additionalServiceProduct in additionalServiceProducts.Where(x => (x.ParentOrderProductProductId ?? x.ProductId) == orderProductGroup.Key))
                {
                    int orderProductId = allOrderProductIdsWithParent.First(z => z.orderProductId == additionalServiceProduct.OrderProductId).parentRecordId ?? 0;

                    if (orderProductId == 0)
                    {
                        continue;
                    }

                    OrderProductPackViewItem item = new OrderProductPackViewItem
                    {
                        Id = orderProductId,
                        IsAdditionalServiceProduct = true,
                        ProductId = product.ProductId,
                        Name = $"({additionalServiceProduct.Id}) {product.FullName}",
                        NameUkr = $"({additionalServiceProduct.Id}) {product.FullNameUa}",
                        NameEn = $"({additionalServiceProduct.Id}) {product.FullNameEn}",
                        ProductFullNameUa = product.FullNameUa,
                        PrintWarrantyCard = product.PrintWarrantyCard,
                        KeepSerial = product.KeepSerial,
                        KeepSerialOverridden = product.KeepSerial,
                        Quantity = 0,
                        WarrantyId = warrantyId,
                        OrderProducts = new[]
                        {
                            new OrderProductQuantityViewItem
                            {
                                OrderProductId = orderProductId,
                                Quantity = 1
                            }
                        }
                    };

                    if (additionalServiceProduct.SerialNumber is not null)
                    {
                        item.Serials.Add(additionalServiceProduct.SerialNumber);
                        usedSerialNumbers.Add(additionalServiceProduct.SerialNumber);
                    }

                    item.Quantity = 1;
                    item.AdditionalServiceProductId = additionalServiceProduct.Id;
                    item.GroupString = $"Товары с оказанными услугами по заказу № {additionalServiceProduct.OrderId}";

                    quantity -= 1;

                    yield return item;
                }

                foreach (AssemblyServiceDto assemblyService in assemblyServices.Where(x => x.OrderProductId.HasValue
                                                                                           && x.ProductId == orderProductGroup.Key
                                                                                           && orderProductIds.Contains(x.OrderProductId.Value)
                                                                                           && !string.IsNullOrWhiteSpace(x.NomenclatureSeries)))
                {
                    if (quantity == 0)
                    {
                        break;
                    }

                    OrderProductPackViewItem item = new OrderProductPackViewItem
                    {
                        Id = assemblyService.OrderProductId!.Value,
                        ProductId = product.ProductId,
                        Name = product.FullName,
                        NameUkr = product.FullNameUa,
                        NameEn = product.FullNameEn,
                        ProductTypeId = product.TypeId,
                        ProductFullNameUa = product.FullNameUa,
                        PrintWarrantyCard = product.PrintWarrantyCard,
                        KeepSerial = product.KeepSerial,
                        KeepSerialOverridden = product.KeepSerial,
                        Quantity = 0,
                        WarrantyId = warrantyId,
                        OrderProducts = new[]
                        {
                            new OrderProductQuantityViewItem
                            {
                                OrderProductId = assemblyService.OrderProductId!.Value,
                                Quantity = 1
                            }
                        }
                    };

                    item.Serials.Add(assemblyService.NomenclatureSeries);
                    usedSerialNumbers.Add(assemblyService.NomenclatureSeries);

                    item.Quantity = 1;
                    item.NomenclatureSeries = assemblyService.NomenclatureSeries;
                    item.GroupString = "Конфигурации ПК";

                    quantity -= 1;

                    yield return item;
                }

                if (quantity > 0)
                {
                    OrderProductPackViewItem item = new OrderProductPackViewItem
                    {
                        IsConsumableAdditionalServiceProduct = false,
                        Id = orderProductIds.First(),
                        ProductId = product.ProductId,
                        Name = product.FullName,
                        NameUkr = product.FullNameUa,
                        NameEn = product.FullNameEn,
                        ProductTypeId = product.TypeId,
                        ProductFullNameUa = product.FullNameUa,
                        PrintWarrantyCard = product.PrintWarrantyCard,
                        KeepSerial = product.KeepSerial,
                        KeepSerialOverridden = product.KeepSerial,
                        Quantity = 0,
                        WarrantyId = warrantyId,
                        OrderProducts = orderProductGroup
                            .Select(x => new OrderProductQuantityViewItem { OrderProductId = x.Id, Quantity = x.Quantity })
                            .ToArray()
                    };

                    if (serialNumbers.Any())
                    {
                        item.Serials.AddRange(serialNumbers.Where(x => !usedSerialNumbers.Contains(x)));
                    }

                    item.Quantity = quantity;
                    item.ScannedQuantity = item.Serials.Count;

                    item.GroupString = "Товары";

                    yield return item;
                }
            }
        }

        private PackageProperties GetPackageProperties(OrderDto orderObj)
        {
            PackageProperties packageProperties = null;

            if (orderObj.CarryId != CarryType.PickupId && orderObj.CarryId != CarryType.UklonId)
            {
                PackageMaxDimensionsParameter maxDimensionsOrder = null;

                ProductSimpleDto[] orderProductsSimpleDtos = orderObj.Products
                    .Select(x => x.Product)
                    .ToArray();

                if (orderProductsSimpleDtos.Length > 0)
                {
                    int width = orderProductsSimpleDtos.Select(x => (int)Math.Ceiling((decimal)(x.Width ?? 0) / 10)).Max();

                    int heigth = orderProductsSimpleDtos.Select(x => (int)Math.Ceiling((decimal)(x.Height ?? 0) / 10)).Max();

                    int depth = orderProductsSimpleDtos.Select(x => (int)Math.Ceiling((decimal)(x.Depth ?? 0) / 10)).Max();

                    maxDimensionsOrder = new PackageMaxDimensionsParameter(heigth, width, depth);
                }

                PackagePropertiesViewModel dialogViewModel = SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                    new PackagePropertiesParameter(
                        orderObj.PackagePlaces,
                        insurance,
                        orderObj.CarryId,
                        (decimal)order.Products.Sum(x => x.Quantity * x.Product.Weight),
                        maxDimensionsParameter: maxDimensionsOrder,
                        products: orderProductsSimpleDtos.Select(x => new PackagePropertiesProductParameter(x.Height, x.Width, x.Depth)).ToArray()),
                    this);

                if (dialogViewModel.IsOk)
                {
                    packageProperties = new PackageProperties(dialogViewModel.PackagePlaceItems.Select(x => new PackagePlaceProperties(x.Weight, x.Insurance, x.Length, x.Height, x.Width)), IsStickerFragile)
                    {
                        AddToApplication = !dialogViewModel.NotAddToNpApplication
                    };
                }
            }
            else
            {
                packageProperties = new PackageProperties(new[] { new PackagePlaceProperties(1, 0) }, IsStickerFragile);
            }

            return packageProperties;
        }

        private void HandlePreviewMouseLeftButtonUp(string fieldName)
        {
            if (OrderProduct != null && string.Equals(fieldName, nameof(OrderProductPackViewItem.PrintWarrantyCard), StringComparison.Ordinal))
            {
                OrderProduct.PrintWarrantyCard = !OrderProduct.PrintWarrantyCard;
            }
        }

        private async Task PrintAcceptanceProtocolAsync()
        {
            await Mediator.Send(new PrintOrderDocumentRequest(order.Id, OrderDocumentType.AcceptanceProtocolId, copies: AcceptanceProtocolQuantity));
        }

        private async Task PrintDocumentsAsync(OrderDto packResult)
        {
            if (IsAutoPrintCheque)
            {
                await Mediator.Send(new PrintOrderDocumentRequest(packResult.Id, OrderDocumentType.ChequeId, todayAsIssueDate: true, preview: false));
            }

            if (NeedToPrintAcceptanceProtocol && IsAutoPrintAcceptanceProtocol && AcceptanceProtocolQuantity > 0)
            {
                await Mediator.Send(new PrintOrderDocumentRequest(packResult.Id, OrderDocumentType.AcceptanceProtocolId, todayAsIssueDate: true, preview: false, copies: AcceptanceProtocolQuantity));
            }

            if (IsAutoPrintPackList)
            {
                await PrintPackListAsync(false);
            }

            if (IsAutoPrintWarrantyCard)
            {
                int[] productIds = OrderProducts
                    .Where(x => x.PrintWarrantyCard)
                    .Select(x => x.ProductId)
                    .ToArray();

                if (productIds.Any())
                {
                    await Mediator.Send(new PrintWarrantyCardRequest(packResult.Id, productIds, null, false));
                }
            }

            if (NeedToPrintTrackNumber && IsAutoPrintTrackNumber)
            {
                ProgressText = "Загрузка ТТН";

                try
                {
                    await trackNumberProvider.PrintAsync(packResult.PackageTtn, false);
                }
                finally
                {
                    ProgressText = string.Empty;
                }
            }

            if (IsStickerFragile)
            {
                await DocumentCommands.PrintStickerFragileCommand.ExecuteAsync(new PrintStickerParameter(packResult.CarryId, false));
            }

            if (IsStickerThisWayUp)
            {
                await DocumentCommands.PrintStickerWayUpCommand.ExecuteAsync(new PrintStickerParameter(packResult.CarryId, false));
            }

            if (AllowBills)
            {
                if (IsAutoPrintBill)
                {
                    await PrintBillAsync();
                }

                if (IsAutoPrintBillInvoice)
                {
                    await PrintBillInvoiceAsync();
                }
            }

            if (IsGuestProductPrint)
            {
                AdditionalServiceProductClientProductReportDataDto data = await WebClient.ExecuteApiRequestAsync(new QueryActAdditionalServiceProductReportData(order.Id));

                AdditionalServiceProductClientProductsReportData reportData = new(DateTime.Now, data.FullNameClient, string.Empty, data.PlaceName, WebClient.AuthenticatedEmployee.Name);

                reportData.SetGuestProducs(data.GuestProducts?.Select(x => Mapper.Map<GuestProductReportData>(x)).ToArray());
                reportData.SetNumber(order.Id);

                IReport report = new ActOutcomeClientProductReport { DataSource = new[] { reportData } };

                PrintReportRequest printRequest = new PrintReportRequest(report, true);

                await Mediator.Send(printRequest);
            }
        }

        private async Task PrintWarrantyCardAsync()
        {
            List<OrderProductPackViewItem> orderProductsToPrint = OrderProducts.Where(x => x.PrintWarrantyCard).ToList();

            if (!orderProductsToPrint.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Выберите хотя бы один товар");
                return;
            }

            if (orderProductsToPrint.Any(x => x.KeepSerial && x.ScannedQuantity != x.Quantity))
            {
                MessageFacadeService.ShowNotificationError(SerialNumberScannedMessage);
                return;
            }

            if (orderProductsToPrint.Any(x => x.ScannedQuantity != x.Quantity))
            {
                MessageFacadeService.ShowNotificationWarning(SerialNumberScannedMessage);
            }

            int[] productIds = orderProductsToPrint.Select(x => x.ProductId).ToArray();

            await Mediator.Send(new PrintWarrantyCardRequest(orderPackInfo.Order.Id, productIds, null, true));
        }

        private async Task PrintPackListAsync(bool showPreview)
        {
            PrintingSettingsInfo printSettings = await PrintingSettings.LoadAsync();

            OrderAssemblyReportSimpleDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryOrderAssemblyReportSimple(orderPackInfo.Order.Id));

            OrderAssemblyReportSimpleData reportData = Mapper.Map<OrderAssemblyReportSimpleData>(reportDto);

            IReport report = new OrderAssemblyReportSimple { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                   ? new PrintReportRequest(report, showPreview, printSettings.Main.Name, printSettings.Main.PaperSource)
                   : new PrintReportRequest(report, showPreview);

            await Mediator.Send(printRequest);
        }

        private void RecognizeBarcodeViewModelOnFinishCommand(object sender, EventArgs e)
        {
            OkCommand.Execute(null);
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            switch (e.Result)
            {
                case RecognizeBarcodeResult.Found:
                case RecognizeBarcodeResult.FoundInSupplier:
                    e.Message = AddProduct(e.Product, e.Quantity);
                    break;
                case RecognizeBarcodeResult.FoundAssembly:

                    OrderProductPackViewItem[] assemblyProducts = OrderProducts.Where(x => x.AssemblyId == e.AssemblyServiceId).ToArray();

                    if (assemblyProducts.Length == 0)
                    {
                        e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Такой сборки нет в заказе");
                        return;
                    }

                    assemblyProducts.ForEach(x => x.ScannedQuantity = x.Quantity);
                    break;
                case RecognizeBarcodeResult.FoundAdditionalServiceProduct:
                    e.Message = AddProduct(e.Product, e.Quantity, e.AdditionalServiceProductId);

                    break;
                case RecognizeBarcodeResult.NotFound:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК не найден в БД");
                    break;
            }
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private async Task<OrderDto> TryPackOrderAsync(OrderPackDto dto)
        {
            OrderDto orderFromServer = null;

            try
            {
                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new TryPackOrder(dto));

                orderFromServer = result.Data;

                if (result.Warnings?.Any() == true)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{orderFromServer.Id.ToString(CultureInfo.InvariantCulture)} упакован с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{orderFromServer.Id.ToString(CultureInfo.InvariantCulture)} успешно упакован");
                }

                Messenger.Send(new OrderMessage(orderFromServer, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }

            return orderFromServer;
        }

        private async Task<bool> HasProductInServiceRequestAsync()
        {
            if (order.BasedOnServiceRequestId == null)
            {
                return false;
            }

            ServiceRequestDto serviceRequest = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(order.BasedOnServiceRequestId.Value)),
                null,
                null,
                this,
                true,
                showDialog: false,
                showError: false,
                showNotification: false);

            int[] productIds = order.Products.Select(x => x.Product.Id).ToArray();

            return productIds.Contains(serviceRequest.ProductId);
        }

        private async Task<Result> FetchAssemblyServicesAsync(int orderId)
        {
            AssemblyServicesFilteringItem filteringItem = new AssemblyServicesFilteringItem()
            {
                SubdivisionId = Subdivision.Telemart.Id,
                StateIds = string.Join(",", new[] { AssemblyServiceState.Completed.Id }),
                OrderStateIds = string.Join(",", new[] { OrderStatus.Received.Id, OrderStatus.Confirmed.Id, OrderStatus.Packed.Id })
            };

            PagedResult<AssemblyServiceDto> allAssemblyServices = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filteringItem));

            _anotherOrdersNomenclatureSeries = allAssemblyServices.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.NomenclatureSeries) && x.OrderId != orderId)
                .Select(x => x.NomenclatureSeries)
                .ToHashSet();

            return Result.Success();
        }
    }
}