using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Reports.AssemblyService;
using Telemart.Client.Reports.Common;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Common.PrintImportStickerParameters;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store.Invoice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.Views.Store;

namespace Telemart.Client.ViewModels.Store
{
    [ViewName(nameof(ProductsComparisonView))]
    public abstract class ProductsComparisonViewModelBase : TelemartDialogViewModelBase
    {
        private const int NoProductId = 0;

        public ProductsComparisonViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IBarcodeReportFactory barcodeReportFactory,
            IMediator mediator,
            IMessenger messenger,
            DocumentCommands documentCommands,
            IPrintingSettingsStore printingSettings)
            : base(webClient, dictionaries, messageFacadeService)
        {
            BarcodeReportFactory = barcodeReportFactory;
            Mediator = mediator;
            Messenger = messenger;
            DocumentCommands = documentCommands;
            PrintingSettings = printingSettings;

            AddProductCommand = new DelegateCommand(AddProduct);
            RemoveProductCommand = new DelegateCommand<ProductComparisonViewItem>(RemoveProduct, x => x?.MinQuantity == 0);
            PrintOurBarcodesCommand = new AsyncCommand<ProductComparisonViewItem>(PrintOurBarcodesAsync, x => x != null);
            PrintImportStickerCommand = new AsyncCommand<ProductComparisonViewItem>(PrintImportStickerAsync, x => x != null);
            EditSerialsCommand = new DelegateCommand<ProductComparisonViewItem>(EditSerials, x => x != null);
            ResetQuantityRealCommand = new DelegateCommand<ProductComparisonViewItem>(ResetQuantityReal, x => x != null);
            SetQuantityRealCommand = new DelegateCommand<ProductComparisonViewItem>(SetQuantityReal, x => x != null);
            EditQuantityRealCommand = new DelegateCommand<ProductComparisonViewItem>(EditQuantityReal, x => x != null);
            WindowClosingCommand = new DelegateCommand<CancelEventArgs>(WindowClosing);
            GenerateSerialNumbersCommand = new DelegateCommand<ProductComparisonViewItem>(GenerateSerialNumbers, x => x != null);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;

            Messenger.Register<ProductCardMessage>(this, OnProductCardChanged);
        }

        public ProductsComparisonViewModelBase()
        {
        }

        #region Commands

        public IDelegateCommand AddProductCommand { get; }

        public IAsyncCommand PrintOurBarcodesCommand { get; }

        public IAsyncCommand PrintImportStickerCommand { get; }

        public IDelegateCommand RemoveProductCommand { get; }

        public IDelegateCommand EditSerialsCommand { get; }

        public IDelegateCommand ResetQuantityRealCommand { get; }

        public IDelegateCommand SetQuantityRealCommand { get; }

        public IDelegateCommand EditQuantityRealCommand { get; }

        public IDelegateCommand WindowClosingCommand { get; }

        public IDelegateCommand GenerateSerialNumbersCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion

        #region INPC

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public Stopwatch ComparisonStopwatch
        {
            get { return GetProperty(() => ComparisonStopwatch); }
            set { SetProperty(() => ComparisonStopwatch, value); }
        }

        public ObservableCollection<ProductComparisonViewItem> ProductComparisonViewItems
        {
            get { return GetProperty(() => ProductComparisonViewItems); }
            set { SetProperty(() => ProductComparisonViewItems, value); }
        }

        public ObservableCollection<InvoiceProductViewItem> InvoiceProducts
        {
            get { return GetProperty(() => InvoiceProducts); }
            set { SetProperty(() => InvoiceProducts, value); }
        }

        public ProductComparisonViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 480;

        public override int MinHeight => 400;

        public override int MinWidth => 600;

        public override int Width => 645;

        #endregion

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public ScanSerialMode ScanSerialMode { get; set; } = ScanSerialMode.Single;

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private IMediator Mediator { get; }

        private IMessenger Messenger { get; }

        private IPrintingSettingsStore PrintingSettings { get; }

        public override void OnDestroy()
        {
            RecognizeBarcodeViewModel.OnStarted -= RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished -= RecognizeBarcodeViewModelOnFinished;

            base.OnDestroy();
        }

        protected virtual List<ProductComparisonResult> GetDeviation()
        {
            Dictionary<int, ProductComparisonResult> items = new Dictionary<int, ProductComparisonResult>();

            foreach (ProductComparisonViewItem gridItem in ProductComparisonViewItems)
            {
                items[gridItem.ProductId] = new ProductComparisonResult(gridItem.FullName, 0, gridItem.QuantityReal);
            }

            foreach (InvoiceProductViewItem invoiceProduct in InvoiceProducts)
            {
                int gridItemQuantity = 0;

                if (items.TryGetValue(invoiceProduct.ProductId, out ProductComparisonResult item))
                {
                    gridItemQuantity = item.QuantityReal;
                }

                items[invoiceProduct.ProductId] = new ProductComparisonResult(
                    invoiceProduct.FullName,
                    invoiceProduct.Quantity,
                    gridItemQuantity);
            }

            return items.Values.Where(x => x.DeviationQuantity != 0).ToList();
        }

        private void AddOrEditProduct(ProductAttributesDto product, int quantity)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            ProductComparisonViewItem gridItem = ProductComparisonViewItems.FirstOrDefault(x => x.ProductId == product.ProductId);

            if (gridItem == null)
            {
                gridItem = new ProductComparisonViewItem(
                    product.ProductId,
                    product.FullNameUa,
                    0,
                    product.KeepSerial,
                    product.SelfBarcode,
                    0,
                    Enumerable.Empty<string>());

                ProductComparisonViewItems.Add(gridItem);
            }

            if (product.KeepSerial)
            {
                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    product.ProductId,
                    gridItem.Serials.ToList(),
                    RecognizeBarcodeViewModel.GetBarcodesById(product.ProductId).ToList(),
                    product.SerialNumberLength,
                    ScanSerialMode,
                    minSerialCount: gridItem.MinQuantity);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    gridItem.AddSerials(viewModel.SerialNumbers);
                    gridItem.QuantityReal = gridItem.Serials.Count;

                    ScanSerialMode = viewModel.ScanMode;
                }
            }
            else
            {
                gridItem.QuantityReal += quantity;
            }

            SelectedProduct = gridItem;
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private void RemoveProduct(ProductComparisonViewItem selectedProduct)
        {
            if (selectedProduct != null)
            {
                RecognizeBarcodeViewModel.RemoveAssignedBarcodes(selectedProduct.ProductId);

                int selectedIndex = ProductComparisonViewItems.IndexOf(selectedProduct);
                SelectedProduct = ProductComparisonViewItems.RemoveAtAndGetNext(selectedIndex);
            }
        }

        private void OnProductCardChanged(ProductCardMessage message)
        {
            if (message.MessageType != MessageType.Changed)
            {
                return;
            }

            ProductCardDto productCard = message.Entity;

            ProductComparisonViewItems.DoActionWithItem(
                x => x.ProductId == productCard.ProductId,
                viewItem =>
                {
                    viewItem.KeepSerial = productCard.KeepSerial;
                    viewItem.SelfBarcode = productCard.SelfBarcode;

                    if (productCard.KeepSerial)
                    {
                        viewItem.QuantityReal = viewItem.Serials.Count;
                    }
                });

            RecognizeBarcodeViewModel.Update(productCard);
        }

        private void AddProduct()
        {
            ChooseProduct(string.Empty, NoProductId, 1);
        }

        private RecognizeBarcodeMessage ChooseProduct(string barcode, int productId, int quantity)
        {
            RecognizeBarcodeMessage message = null;

            bool alive;

            Tuple<int, string>[] items = GetChooseProductItems().ToArray();

            do
            {
                ProductSelectionViewModel selectionViewModel = DialogDocumentManagerService.ShowView<ProductSelectionViewModel>(
                    new object[] { items, productId },
                    this);

                if (!selectionViewModel.IsOk)
                {
                    break;
                }

                productId = selectionViewModel.SelectedProduct.Item1;

                if (productId == NoProductId)
                {
                    message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Оприходуйте товар отдельно в 1С за 100 000");
                    break;
                }

                ProductAttributesDto product = RecognizeBarcodeViewModel.FindById(productId);

                if (product.SelfBarcode)
                {
                    AddOrEditProduct(product, quantity);
                    message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "У данного товара наш ШК. Распечатайте его вручную.");
                    break;
                }

                ProductSelectionValidationViewModel confirmationViewModel = DialogDocumentManagerService.ShowView<ProductSelectionValidationViewModel>(
                    new ProductSelectionValidationViewModelParameter(barcode, product.GetLocalName(LocalizableNameType.Ukr), product.Barcodes?.Select(x => x.Barcode).ToArray()),
                    this);

                if (confirmationViewModel.IsOk)
                {
                    if (!string.IsNullOrWhiteSpace(barcode))
                    {
                        RecognizeBarcodeViewModel.AddBarcodeAssignment(barcode, productId);
                    }

                    AddOrEditProduct(product, quantity);
                    message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Info, $"Сопоставлено: {product.FullName} ({barcode})");
                    break;
                }

                alive = confirmationViewModel.IsNo;
            }
            while (alive);

            return message;
        }

        private IEnumerable<Tuple<int, string>> GetChooseProductItems()
        {
            yield return new Tuple<int, string>(NoProductId, "Товара нет в накладной");

            foreach (InvoiceProductViewItem x in InvoiceProducts.OrderBy(x => x.GetLocalFullName(LocalizableNameType.Ukr)))
            {
                yield return new Tuple<int, string>(x.ProductId, x.GetLocalFullName(LocalizableNameType.Ukr));
            }
        }

        private async Task PrintOurBarcodesAsync(ProductComparisonViewItem selectedProduct)
        {
            int defaultCopies = Math.Max(1, selectedProduct.QuantityReal);
            int defaultQuantity = 1;

            PrintBarcodeParametersParameter parameter = new PrintBarcodeParametersParameter(defaultQuantity, defaultCopies);

            PrintBarcodeParametersViewModel viewModel = DialogDocumentManagerService.ShowView<PrintBarcodeParametersViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                short copies = (short)viewModel.Count;
                int quantity = (int)viewModel.Quantity;

                BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(viewModel.Format, selectedProduct.FullName, selectedProduct.ProductId, quantity);

                if (result.Printer == null)
                {
                    MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                    return;
                }

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    result.Report,
                    false,
                    result.Printer.Name,
                    result.Printer.PaperSource,
                    copies);

                await Mediator.Send(printReportRequest);
            }
        }

        private async Task PrintImportStickerAsync(ProductComparisonViewItem selectedProduct)
        {
            PrintingSettingsInfo printingSettingsInfo = await PrintingSettings.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettingsInfo?.Barcode50X40;

            if (string.IsNullOrEmpty(printerSettings?.Name))
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(new QueryProductByIdsDto(new[] { selectedProduct.ProductId }, selectedProduct.SupplierId)));

            if (products?.Count >= 0)
            {
                CategoryFullDto category = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(products.First().CategoryId));

                if (!CanPrintImportSticker(category))
                {
                    return;
                }

                string nameFeature = string.Empty;

                if (category.FeatureId.HasValue)
                {
                    FeatureFullDto featureFullDto =
                        await WebClient.ExecuteApiRequestAsync(new QueryFeature(category.FeatureId.Value));

                    if (featureFullDto == null)
                    {
                        MessageFacadeService.ShowNotificationWarning($"Характеристика №{category.FeatureId} указанная в категории {category.NameFull} не найдена");
                        return;
                    }

                    List<ProductFeatureGroupsDto> productFeatureGroupsDtos = await WebClient.ExecuteApiRequestAsync(new QueryProductFeatures(new[] { products.First().Id }));

                    ReadOnlyObservableCollection<ProductFeatureDto> productFeatureDtos = productFeatureGroupsDtos?
                        .SelectMany(x => x.AttributeGroups)
                        .SelectMany(x => x.Attributes)
                        .ToReadOnlyObservableCollection();

                    ProductFeatureDto productFeatureDto = productFeatureDtos?.FirstOrDefault(x => x.Id == featureFullDto.Id);

                    string nameFeatureValue = productFeatureDto?.Value;

                    nameFeature = $"{featureFullDto.Name} : {nameFeatureValue}";
                }

                int defaultCopies = Math.Max(1, selectedProduct.QuantityReal);

                PrintImportStickerParametersParameter parameter = new PrintImportStickerParametersParameter(defaultCopies);

                PrintImportStickerParametersViewModel viewModel = DialogDocumentManagerService.ShowView<PrintImportStickerParametersViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    short copies = (short)viewModel.Count;

                    TelemartAddressDto telemartAddressDto = await WebClient.ExecuteApiRequestAsync(new QueryTelemartAddress());

                    StickerProductReportData reportData = new StickerProductReportData(
                        selectedProduct.FullName,
                        nameFeature,
                        DateTime.Today.AddMonths(-2),
                        category.MarkerManufacture ?? string.Empty,
                        category.MarkerManufactureAddress ?? string.Empty,
                        "ТОВ \"ТЕЛЕМАРТ\"",
                        telemartAddressDto?.Address,
                        telemartAddressDto?.Phone,
                        telemartAddressDto?.Email);

                    StickerProductReport report = new StickerProductReport
                    {
                        DataSource = new[] { reportData }
                    };

                    PrintReportRequest printReportRequest = new PrintReportRequest(
                        report,
                        true,
                        printerSettings.Name,
                        printerSettings.PaperSource,
                        copies);

                    await Mediator.Send(printReportRequest);
                }
            }
        }

        private bool CanPrintImportSticker(CategoryFullDto category)
        {
            if (category == null)
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"Категория не найдена");

                return false;
            }

            if (category.MarkerManufacture == null
                && category.MarkerManufactureAddress == null
                && category.FeatureId == null)
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено маркировка товара");

                return false;
            }

            if (string.IsNullOrEmpty(category.MarkerManufacture))
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено Название компании в блоке маркировка товара");

                return false;
            }

            if (string.IsNullOrEmpty(category.MarkerManufactureAddress))
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено Адрес в блоке маркировка товара");

                return false;
            }

            return true;
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
                    AddOrEditProduct(e.Product, e.Quantity);
                    break;
                case RecognizeBarcodeResult.FoundInSupplier:
                    e.Message = ChooseProduct(e.BarcodeText, e.ProductId.Value, e.Quantity);
                    break;
                case RecognizeBarcodeResult.NotFound:
                    e.Message = ChooseProduct(e.BarcodeText, NoProductId, e.Quantity);
                    break;
            }
        }

        private void EditQuantityReal(ProductComparisonViewItem gridItem)
        {
            if (gridItem.KeepSerial)
            {
                EditSerialsCommand.Execute(gridItem);
            }
            else
            {
                ResetQuantityRealCommand.Execute(gridItem);
            }
        }

        private void ResetQuantityReal(ProductComparisonViewItem gridItem)
        {
            if (MessageFacadeService.Confirm("Очистить текущее значение?"))
            {
                gridItem.ClearSerials();
                gridItem.QuantityReal = 0;
            }
        }

        private void SetQuantityReal(ProductComparisonViewItem gridItem)
        {
            GetTextFromUserParameter parameter = new GetTextFromUserParameter(
                "Количество",
                "Введите количество",
                @"^[1-9]\d{0,3}$",
                "Введите положительное целое число");

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                gridItem.ClearSerials();
                gridItem.QuantityReal = int.Parse(viewModel.Content);
            }
        }

        private void EditSerials(ProductComparisonViewItem gridItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(gridItem.Serials.ToList()), this);

            if (viewModel.IsOk)
            {
                gridItem.ReplaceSerials(viewModel.SerialNumbers);
                gridItem.QuantityReal = gridItem.Serials.Count;
            }
        }

        private void GenerateSerialNumbers(ProductComparisonViewItem item)
        {
            if (!item.KeepSerial)
            {
                MessageFacadeService.ShowNotificationError("По товару не ведется учет серийных номеров");
                return;
            }

            DialogDocumentManagerService.ShowView<GenerateSerialNumbersViewModel>(new GenerateSerialNumbersParameter(item.ProductId, item.FullName), this);
        }

        private void WindowClosing(CancelEventArgs e)
        {
            e.Cancel = !IsOk && !MessageFacadeService.Confirm("Вы уверены, что хотите выйти?");
        }
    }
}