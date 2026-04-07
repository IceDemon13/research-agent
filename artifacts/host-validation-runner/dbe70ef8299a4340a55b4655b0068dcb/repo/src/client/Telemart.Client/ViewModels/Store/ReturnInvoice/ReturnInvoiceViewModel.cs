using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Purchase;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.Requests.Features.ReturnInvoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.ReturnInvoice;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class ReturnInvoiceViewModel : TelemartEditorViewModelBase<ReturnInvoiceDto, ReturnInvoiceParameter, ReturnInvoiceViewItem>
    {
        private readonly TelegramBotOptions _telegramBotOptions;

        private IReadOnlyDictionary<int, List<string>> _accountingSystemSerials;
        private IReadOnlyDictionary<int, string[]> _invoiceSerialNumbersByProduct;
        private ScanSerialMode _scanSerialMode = ScanSerialMode.Single;
        private IReadOnlyDictionary<string, string> _npContractorNames;

        private ITrackNumberProvider _trackNumberProvider;

        public ReturnInvoiceViewModel(
           IWebClient webClient,
           IMessageFacadeService messageFacadeService,
           IMessenger messenger,
           IMapper mapper,
           IPriceConverterFactory priceConverterFactory,
           IDictionaries dictionaries,
           DocumentCommands documentCommands,
           IPrintingSettingsStore printingSettingsStore,
           IMediator mediator,
           IErrorHandler errorHandler,
           ProductInformationViewModel productInformationViewModel,
           TelegramBotOptions telegramBotOptions)
           : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            ErrorHandler = errorHandler;
            PriceConverterFactory = priceConverterFactory;
            ProductInformation = productInformationViewModel;
            _telegramBotOptions = telegramBotOptions;

            SendReturnInvoiceCommand = new AsyncCommand(SendReturnInvoiceAsync, () => Model?.EmployeeLockId == null && Model?.StateId == ReturnInvoiceState.Confirmed.Id && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceSend));
            CancelReturnInvoiceCommand = new AsyncCommand(CancelReturnInvoiceAsync, () => Model?.EmployeeLockId == null && Model?.StateId != ReturnInvoiceState.Canceled.Id && Model?.StateId != ReturnInvoiceState.Sent.Id && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceCancel));
            WaitingConfirmCommand = new AsyncCommand(WaitingConfirmAsync);
            ConfirmCommand = new AsyncCommand(ConfirmAsync, () => Model?.EmployeeLockId == null && Model?.StateId == ReturnInvoiceState.WaitingConfirm.Id && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceConfirm));
            PrintSnCommand = new AsyncCommand(PrintSnAsync, () => Model?.StateId != ReturnInvoiceState.Canceled.Id && Model?.StateId != ReturnInvoiceState.Sent.Id);
            PrintReturnInvoiceAssemblyCommand = new AsyncCommand(PrintReturnInvoiceAssemblyAsync);
            PrintReturnInvoiceSupplierCommand = new AsyncCommand<short>(PrintReturnInvoiceSupplierAsync);

            SwitchDocumentsControlCommand = new AsyncCommand(SwitchDocumentsControlAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceSwitchDocumentsControl) && Model?.StateId != ReturnInvoiceState.Canceled.Id && Model?.StateId != ReturnInvoiceState.Sent.Id);
            SetNpContractorCommand = new AsyncCommand(SetSenderNpContractorAsync, () => WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceSetSenderNpContractor) && Model?.StateId != ReturnInvoiceState.Canceled.Id && Model?.StateId != ReturnInvoiceState.Sent.Id);
            EditProductSerialsCommand = new DelegateCommand<ReturnInvoiceProductViewItem>(EditProductSerials);
            ClearScannedQuantityCommand = new DelegateCommand<ReturnInvoiceProductViewItem>(ClearScannedQuantity, _ => IsLockedByCurrentEmployee);

            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
            HandleTabSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleTabSelectionChanged);
            RemoveDocumentCommand = new AsyncCommand<ReturnInvoiceDocumentSimpleDto>(RemoveDocumentAsync, x => x != null && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceDeleteDocument));
            AddDocumentCommand = new AsyncCommand(AddDocumentAsync, () => Model != null && Model.DocumentsControl && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceCreateDocument));
            RefreshDocumentsCommand = new AsyncCommand(RefreshDocumentsAsync, () => Model != null && Model.DocumentsControl);
            PrintDocumentCommand = new AsyncCommand<ReturnInvoiceDocumentSimpleDto>(PrintDocumentAsync, x => x != null);
            PrintTtnCommand = new AsyncCommand(PrintTrackNumberAsync, () => !string.IsNullOrEmpty(Model?.TrackNumber));
            EditInvoiceCommand = new DelegateCommand(EditInvoice);
            ShowDocumentsBotQrCommand = new DelegateCommand(ShowDocumentsBotQr, () => Model?.Id > 0);

            DocumentCommands = documentCommands;
            Documents = new ObservableCollection<ReturnInvoiceDocumentSimpleDto>();
            Warehouses = new ObservableRangeCollection<WarehouseDto>();
            Carries = new ObservableRangeCollection<CarryDto>();
            Employees = new ObservableRangeCollection<EmployeeDto>();

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;

            Messenger.Register<ReturnInvoiceDocumentMessage>(this, OnDocumentMessage);
        }

        public ReturnInvoiceViewModel(TelegramBotOptions telegramBotOptions)
        {
            _telegramBotOptions = telegramBotOptions;
        }

        #region Commands

        public IAsyncCommand SendReturnInvoiceCommand { get; }

        public IAsyncCommand WaitingConfirmCommand { get; }

        public IAsyncCommand ConfirmCommand { get; }

        public IAsyncCommand PrintSnCommand { get; }

        public IAsyncCommand PrintReturnInvoiceAssemblyCommand { get; }

        public IAsyncCommand PrintReturnInvoiceSupplierCommand { get; }

        public IAsyncCommand SwitchDocumentsControlCommand { get; }

        public IAsyncCommand SetNpContractorCommand { get; }

        public IAsyncCommand CancelReturnInvoiceCommand { get; }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public IDelegateCommand HandleTabSelectionChangedCommand { get; }

        public IDelegateCommand EditInvoiceCommand { get; }

        public IDelegateCommand EditProductSerialsCommand { get; }

        public IDelegateCommand ClearScannedQuantityCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand AddDocumentCommand { get; }

        public IAsyncCommand PrintDocumentCommand { get; }

        public IAsyncCommand RefreshDocumentsCommand { get; }

        public IAsyncCommand RemoveDocumentCommand { get; }

        public IAsyncCommand PrintTtnCommand { get; }

        public IDelegateCommand ShowDocumentsBotQrCommand { get; }

        #endregion

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public ReturnInvoiceProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, RefreshValues); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public int CategoryTypeGroupIndex
        {
            get { return GetProperty(() => CategoryTypeGroupIndex); }
            set { SetProperty(() => CategoryTypeGroupIndex, value); }
        }

        public bool AllowEditQuantity => IsLockedByCurrentEmployee && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceAllowEditQuantityInNewState);

        public bool WaitingConfirmEnabled => Model?.Products.Any(x => x.OutQuantity > 0) == true;

        public bool WaitingConfirmVisible => Model?.EmployeeLockId == null && Model?.StateId == ReturnInvoiceState.New.Id && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceWaitingConfirm);

        #region Collections

        public IEnumerable<SummaryViewItem> ReturnInvoiceSummaryItems
        {
            get { return GetProperty(() => ReturnInvoiceSummaryItems); }
            private set { SetProperty(() => ReturnInvoiceSummaryItems, value); }
        }

        public ReadOnlyObservableCollection<ReturnInvoiceState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ObservableRangeCollection<CarryDto> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ObservableRangeCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<CategoryType> CategoryTypes
        {
            get { return GetProperty(() => CategoryTypes); }
            private set { SetProperty(() => CategoryTypes, value); }
        }

        public ObservableCollection<ReturnInvoiceDocumentSimpleDto> Documents
        {
            get { return GetProperty(() => Documents); }
            private set { SetProperty(() => Documents, value); }
        }

        #endregion

        #region DialogSettings

        public override int Width => 900;

        public override int MinWidth => 900;

        public override int MaxWidth => 1920;

        public override int Height => 550;

        public override int MinHeight => 550;

        public override int MaxHeight => 1080;

        #endregion

        protected override string EntityName => "Возврат";

        protected override string UpdatedActionMessage => "сохранена";

        protected override string CreatedActionMessage => string.Empty;

        private DocumentCommands DocumentCommands { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        private IPriceConverterFactory PriceConverterFactory { get; }

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>();

        private IDocumentManagerService SizeableNotMinimizeDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableNotMinimizeDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        protected override bool CanEdit()
        {
            return
                Model?.StateId == ReturnInvoiceState.New.Id
                && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceUpdate);
        }

        protected override Task<ReturnInvoiceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryReturnInvoice(id));
        }

        protected override Task<LockResponse<ReturnInvoiceDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockReturnInvoice(id));
        }

        protected override Task<LockResponse<ReturnInvoiceDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockReturnInvoice(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"Возврат №{Model.Id}";
        }

        protected override Task<Result<ReturnInvoiceDto>> UpdateEntityAsync()
        {
            ReturnInvoiceSaveDto saveDto = new ReturnInvoiceSaveDto
            {
                Id = Model.Id,
                Products = Model.Products
                    .Select(x => new ReturnInvoiceProductSaveDto
                    {
                        ProductId = x.ProductId,
                        OutQuantity = x.OutQuantity,
                        Quantity = x.Quantity,
                        SerialNumbers = x.SerialNumbers
                    })
                    .ToList()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateReturnInvoice(saveDto));
        }

        protected override Task<Result<ReturnInvoiceDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(ReturnInvoiceViewItem.DocumentsCount);
            yield return nameof(ReturnInvoiceProductViewItem.MaxQuantity);
            yield return nameof(ReturnInvoiceProductViewItem.SerialsQuantity);
            yield return nameof(ReturnInvoiceProductViewItem.StockQuantity);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);
            List<CarryDto> carries = await WebClient.ExecuteApiRequestAsync(new QueryCarries());
            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);
            List<NpContractorDto> npContractors = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(), true);

            _npContractorNames = npContractors.ToDictionary(x => x.Id, x => x.Name);

            Warehouses.AddRange(warehouses.Data);
            Employees.AddRange(employees.Data);
            Carries.AddRange(carries);

            States = Dictionaries.GetItems<ReturnInvoiceState>().ToReadOnlyObservableCollection();

            CategoryTypes = Dictionaries.GetItems<CategoryType>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            PagedResult<InvoiceDto> invoices = await WebClient.ExecuteApiRequestAsync(new QueryInvoices(new InvoiceFilteringItem()
                {
                    InvoicesIds = Model.InvoiceId.ToString()
                }));

            InvoiceDto invoice = invoices.Data.FirstOrDefault();

            if (invoice is null)
            {
                MessageFacadeService.ShowNotificationError("У Вас нет прав на склад принятия накладной");
                Close();
                return;
            }

            _invoiceSerialNumbersByProduct = invoice.InvoiceProducts
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.SelectMany(z => z.SerialNumbers).ToArray());

            _trackNumberProvider = Dictionaries.GetItemById<CarryType>(Model.CarryId).GetTrackNumberProvider();

            RaisePropertiesChanged(nameof(WaitingConfirmEnabled), nameof(WaitingConfirmVisible));

            int[] productIds = Model.Products.Select(x => x.ProductId).ToArray();

            if (productIds.Length != 0)
            {
                PagedResult<ProductAttributesDto> attributesResult = await WebClient.ExecuteApiRequestAsync(new QueryProductsAttributesByIds(productIds, true));

                RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), attributesResult?.Data);

                _accountingSystemSerials = attributesResult?.Data.ToDictionary(x => x.ProductId, x => x.Serials);
            }
            else
            {
                RecognizeBarcodeViewModel.Init(new RecognizeBarcodeSettings(true, true), Array.Empty<ProductAttributesDto>());
            }
        }

        protected override void BeforeSetData(ReturnInvoiceViewItem model, object dto)
        {
            ReturnInvoiceReportDataDto returnInvoiceReportDataDto = WebClient.ExecuteApiRequest(new QueryReturnInvoiceSerialsPrintReport(model.InvoiceId, model.Id));

            IReadOnlyDictionary<int, int> productSerialNumberQuantity = returnInvoiceReportDataDto.Products
                .GroupBy(x => x.ProductId)
                .ToDictionary(x => x.Key, x => x.Count());

            ProductSourceDto[] productSources = WebClient.ExecuteApiRequest(new QueryPurchaseSources(new QueryPurchaseSources.PurchaseSourcesRequest()
            {
                WarehouseIds = new[] { model.WarehouseId },
                IncludeInvoices = false,
                IncludeTransits = false,
                StockStrategy = StockStrategy.Database,
                ProductIds = model.Products.Select(x => x.ProductId).ToArray()
            }));

            IReadOnlyDictionary<int, int> productSource = productSources
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y => y.Quantity
                                    - y.ReservedByOrder
                                    - y.ReservedByShowcase
                                    - y.ReservedByAssemblyComplectation
                                    - y.ReservedByReturnInvoice));

            foreach (ReturnInvoiceProductViewItem product in model.Products)
            {
                product.SerialsQuantity = productSerialNumberQuantity.GetValueOrDefault(product.ProductId);
                product.StockQuantity = Math.Max(0, productSource.GetValueOrDefault(product.ProductId, 0) + (product.AcceptQuantity ?? product.Quantity));
            }

            base.BeforeSetData(model, dto);
        }

        protected override void AfterSetData()
        {
            ReturnInvoiceSummaryItems = GetSummaryItems();

            CategoryTypeGroupIndex = 0;

            RaisePropertiesChanged(nameof(WaitingConfirmVisible), nameof(AllowEditQuantity));
        }

        private void RefreshValues()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, SelectedProduct.CurrencyId);
            }
        }

        private void EditInvoice()
        {
            Messenger.Send(new InvoiceEditViewMessage(Model.InvoiceId));
        }

        private async Task AddDocumentAsync()
        {
            const int maxDocumentsCount = 25;
            const int maxFileLengthMb = 7;

            if (Documents.Count >= maxDocumentsCount)
            {
                MessageFacadeService.ShowNotificationWarning($"К возврату можно добавить не больше чем {maxDocumentsCount} файлов");
                return;
            }

            if (OpenFileDialogService.ShowDialog())
            {
                if (OpenFileDialogService.Files.Count() + Documents.Count > maxDocumentsCount)
                {
                    MessageFacadeService.ShowNotificationWarning($"К возврату можно добавить не больше чем {maxDocumentsCount} файлов");
                    return;
                }

                if (!OpenFileDialogService.Files.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Выберите хотя бы 1 файл");
                    return;
                }

                List<IFileInfo> notValidFiles = OpenFileDialogService.Files.Where(x => x.Length > maxFileLengthMb.Megabytes().Bytes).ToList();

                if (notValidFiles.Any())
                {
                    ShowValidationResultView(
                        "Ошибки при добавлении файлов",
                        notValidFiles.Select(x => new ValidationResultItem($"Файл \"{x.GetFullName()}\" должен быть меньше {maxFileLengthMb} MB", true)).ToArray());
                }
                else
                {
                    AddDocumentsParameter parameter = new AddDocumentsParameter(
                        Model.Id,
                        OpenFileDialogService.Files.Select(x => x.GetFullName()).ToList());

                    ReturnInvoiceAddDocumentViewModel viewModel = new ReturnInvoiceAddDocumentViewModel(
                        WebClient,
                        Dictionaries,
                        MessageFacadeService,
                        Messenger);

                    NonModalSizeableDialogDocumentManagerService.ShowView("AddDocumentsView", viewModel, parameter, this);

                    ReturnInvoiceDto dto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoice(Model.Id));
                    Messenger.Send(new ReturnInvoiceMessage(dto, MessageType.Changed));
                }
            }
        }

        private async Task RemoveDocumentAsync(ReturnInvoiceDocumentSimpleDto document)
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new DeleteReturnInvoiceDocument(Model.Id, document.Id));

                Documents.Remove(document);
                Model.DocumentsCount = Documents.Count;
                MessageFacadeService.ShowNotificationInfo("Документ успешно удален");
                ReturnInvoiceDto dto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoice(Model.Id));
                Messenger.Send(new ReturnInvoiceMessage(dto, MessageType.Changed));
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении документа");
            }
        }

        private async Task RefreshDocumentsAsync()
        {
            Documents.Clear();

            try
            {
                QueryReturnInvoiceDocuments gatewayRequest = new QueryReturnInvoiceDocuments(Model.Id);
                List<ReturnInvoiceDocumentSimpleDto> documents = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                Documents = new ObservableCollection<ReturnInvoiceDocumentSimpleDto>(documents);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get return invoice documents");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task PrintDocumentAsync(ReturnInvoiceDocumentSimpleDto document)
        {
            ReturnInvoiceDocumentDto documentDto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoiceDocument(document.Id));
            await FileHelper.OpenAsFileAsync(documentDto.Data, documentDto.Ext);
        }

        private void EditProductSerials(ReturnInvoiceProductViewItem productViewItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(productViewItem.SerialNumbers.ToList(), !IsLockedByCurrentEmployee), this);

            if (viewModel.IsOk)
            {
                productViewItem.OutQuantity = viewModel.SerialNumbers.Count;

                productViewItem.SerialNumbers = viewModel.SerialNumbers.ToList();
            }
        }

        private void ClearScannedQuantity(ReturnInvoiceProductViewItem productViewItem)
        {
            productViewItem.OutQuantity = 0;
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            if (string.Equals(e.FieldName, nameof(ReturnInvoiceProductViewItem.OutQuantity), StringComparison.Ordinal))
            {
                EditProductSerialsCommand.Execute((ReturnInvoiceProductViewItem)e.Data);
            }
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
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
                case RecognizeBarcodeResult.NotFound:
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "ШК не найден в БД");
                    break;
            }
        }

        private void RecognizeBarcodeViewModelOnFinishCommand(object sender, EventArgs e)
        {
            OkCommand.Execute(null);
        }

        private RecognizeBarcodeMessage AddProduct(ProductAttributesDto product, int quantity)
        {
            ReturnInvoiceProductViewItem[] productViewItems = Model.Products.Where(x => x.ProductId == product.ProductId).ToArray();

            if (!productViewItems.Any())
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товара {product.FullName} нет в возврате");
            }

            if (productViewItems.All(x => x.OutQuantity >= x.Quantity))
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товар {product.FullName} уже просканирован");
            }

            ReturnInvoiceProductViewItem productViewItem = productViewItems.First(x => x.OutQuantity < x.Quantity);

            if (productViewItem.KeepSerial)
            {
                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productViewItem.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    productViewItem.ProductId,
                    productViewItem.SerialNumbers.ToList(),
                    barcodes,
                    product.SerialNumberLength,
                    _scanSerialMode,
                    _accountingSystemSerials[productViewItem.ProductId],
                    validSerialNumbers: _invoiceSerialNumbersByProduct.GetValueOrDefault(productViewItem.ProductId, null),
                    errorForNotValidSerialNumbers: "SN не найден в накладной",
                    maxSerialCount: productViewItem.Quantity);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    productViewItem.SerialNumbers.AddRange(viewModel.SerialNumbers);
                    productViewItem.OutQuantity = productViewItem.SerialNumbers.Count;

                    _scanSerialMode = viewModel.ScanMode;
                }
            }
            else
            {
                productViewItem.OutQuantity += quantity;
            }

            SelectedProduct = productViewItem;

            Model.Products.First(x => x.Id == SelectedProduct.Id).OutQuantity = SelectedProduct.OutQuantity;

            RaisePropertyChanged(nameof(WaitingConfirmEnabled));

            return null;
        }

        private void HandleTabSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "Tab2":

                    if (Documents?.Any() == false)
                    {
                        RefreshDocumentsCommand.Execute(null);
                    }

                    break;
            }
        }

        private async Task WaitingConfirmAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            const string errorRu = "Ошибка при отправке на согласование возврата";
            const string errorEn = "Failed to waiting confirm return invoice";

            try
            {
                WaitingConfirmReturnInvoiceDto request = new WaitingConfirmReturnInvoiceDto();

                if (Model.BitrixId == null)
                {
                    GetDateTimeFromUserParameter parameter = new GetDateTimeFromUserParameter("Создание задачи bitrix", "Дедлайн");

                    GetDateTimeFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetDateTimeFromUserViewModel>(parameter, this);

                    if (!viewModel.IsOk)
                    {
                        return;
                    }

                    request.BitrixDeadLine = viewModel.DateTime.Value;
                }

                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new WaitingConfirmReturnInvoice(Model.Id, request));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Возврат отправлен на согласование с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Возврат успешно отправлен на согласование");
                }

                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                Logger.LogError(exception, errorEn);
            }

            splashScreenManager.Close();
        }

        private async Task ConfirmAsync()
        {
            const string errorRu = "Ошибка при согласовании возврата";
            const string errorEn = "Failed to confirm return invoice";

            ReturnInvoiceProductConfirmParameter parameter = new ReturnInvoiceProductConfirmParameter(Model.Id, Model.Products.Select(x => new ReturnInvoiceProductConfirmViewItem()
            {
                ReturnInvoiceProductId = x.Id,
                ProductName = x.ProductFullName,
                ScannedQuantity = x.OutQuantity,
                ConfirmQuantity = x.OutQuantity,
                SerialNumbers = x.SerialNumbers
            }).ToObservableCollection());

            ReturnInvoiceConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<ReturnInvoiceConfirmViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmReturnInvoice(Model.Id, new ConfirmReturnInvoiceDto(viewModel.Products.Select(x => new ConfirmReturnInvoiceProductDto(x.ReturnInvoiceProductId, x.ConfirmQuantity)).ToList())));
                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));
                Close();

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Предупреждения";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Возврат успешно согласован");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                Logger.LogError(exception, errorEn);
            }
        }

        private PackageProperties GetPackageProperties(int packagePlaces, decimal insurance)
        {
            PackageProperties packageProperties = null;

            if (Model.CarryId != CarryType.PickupId)
            {
                PackagePropertiesViewModel dialogViewModel = SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                    new PackagePropertiesParameter(packagePlaces, insurance, Model.CarryId),
                    this);

                if (dialogViewModel.IsOk)
                {
                    packageProperties = new PackageProperties(dialogViewModel.PackagePlaceItems.Select(x => new PackagePlaceProperties(x.Weight, x.Insurance)), false) { AddToApplication = !dialogViewModel.NotAddToNpApplication };
                }
            }
            else
            {
                packageProperties = new PackageProperties(new[] { new PackagePlaceProperties(1, 0) }, false);
            }

            return packageProperties;
        }

        private async Task SendReturnInvoiceAsync()
        {
            bool successSend = false;

            try
            {
                if (Model.CarryId == CarryType.NpDeliveryId || Model.CarryId == CarryType.NpWarehouseId)
                {
                    IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

                    decimal insurance = Model.Products.Sum(x => x.OutQuantity * priceConverter.Convert(x.Price, x.CurrencyId, Currency.Uah.Id, x.UsdCurrency));

                    PackageProperties packageProperties = GetPackageProperties(1, insurance);

                    if (packageProperties == null)
                    {
                        return;
                    }

                    await WebClient.ExecuteApiRequestAsync(new CreateNpTtnByReturnInvoice(Model.Id, packageProperties.PlaceCount, packageProperties.TotalWeight, packageProperties.AddToApplication));
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ТТН");

                if (!ShowValidationResultView("Ошибки при создании ТТН", exception.GetErrorItems(false)))
                {
                    return;
                }
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create novaposhta TTN");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                return;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании ТТН");
                Logger.LogError(exception, "Error while creating novaposhta TTN");
                return;
            }

            try
            {
                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SendReturnInvoice(Model.Id));

                if (!string.IsNullOrWhiteSpace(result.Data.TrackNumber))
                {
                    await _trackNumberProvider.PrintAsync(result.Data.TrackNumber, true);
                }

                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));

                SetData(result.Data);

                MessageFacadeService.ShowNotificationInfo("Возврат успешно отправлен");

                successSend = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отправке возврата");
                ShowValidationResultView("Ошибки при отправке возврата", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to send return invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отправке возврата");
                Logger.LogError(exception, "Error while sending return invoice");
            }

            if (!successSend)
            {
                Close();
                return;
            }

            try
            {
                if (Documents.Any())
                {
                    List<Task<ReturnInvoiceDocumentDto>> documentTasks = new List<Task<ReturnInvoiceDocumentDto>>();

                    foreach (ReturnInvoiceDocumentSimpleDto document in Documents)
                    {
                        documentTasks.Add(WebClient.ExecuteApiRequestAsync(new QueryReturnInvoiceDocument(document.Id)));
                    }

                    ReturnInvoiceDocumentDto[] documents = await Task.WhenAll(documentTasks);

                    foreach (ReturnInvoiceDocumentDto document in documents)
                    {
                        await FileHelper.OpenAsFileAsync(document.Data, document.Ext);
                    }
                }
                else
                {
                    await PrintReturnInvoiceSupplierAsync(2);
                }

                Close();
                MessageFacadeService.ShowNotificationInfo("Документы успешно открыты");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print return invoice documents");
                MessageFacadeService.ShowNotificationWarning("Ошибка при печати документов");
            }
        }

        private async Task CancelReturnInvoiceAsync()
        {
            bool confirm = MessageFacadeService.Confirm("Вы уверены?", "Отмена возврата");

            if (!confirm)
            {
                return;
            }

            try
            {
                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CancelReturnInvoice(Model.Id));
                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));
                Close();
                MessageFacadeService.ShowNotificationInfo("Возврат успешно отменен");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене возврата");
                ShowValidationResultView("Ошибка при отмене возврата", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to cancel return invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене возврата");
                Logger.LogError(exception, "Error while canceling return invoice");
            }
        }

        private async Task PrintSnAsync()
        {
            (PrintingSettingsInfo printingSettings, PrinterSettingsInfo printerSettings) printSettings = await GetPrintSettingsAsync();

            if (printSettings == default)
            {
                return;
            }

            ReturnInvoiceReportDataDto dataDto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoiceSerialsPrintReport(Model.InvoiceId, Model.Id));

            if (!dataDto.Products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет товара для печати");
                return;
            }

            ReturnInvoiceReportData data = Mapper.Map<ReturnInvoiceReportData>(dataDto);

            IReport report = new ReturnInvoiceReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, true, printSettings.printerSettings.Name, printSettings.printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task PrintReturnInvoiceAssemblyAsync()
        {
            (PrintingSettingsInfo printingSettings, PrinterSettingsInfo printerSettings) printSettings = await GetPrintSettingsAsync();

            if (printSettings == default)
            {
                return;
            }

            ReturnInvoiceAssemblyReportData data = new ReturnInvoiceAssemblyReportData(
                Model.Products
                    .GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(x => new ReturnInvoiceAssemblyReportCategoryData(
                        x.Key.CategoryName,
                        x.Select(z =>
                            new ReturnInvoiceAssemblyReportProductData(
                                z.ProductId,
                                z.FullName,
                                z.Quantity)).ToList()))
                    .ToList(),
                Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId)?.Name,
                Carries.FirstOrDefault(x => x.Id == Model.CarryId)?.Name,
                Model.SupplierName,
                Model.Id);

            IReport report = new ReturnInvoiceAssemblyReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, true, printSettings.printerSettings.Name, printSettings.printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task PrintReturnInvoiceSupplierAsync(short copies = 1)
        {
            (PrintingSettingsInfo printingSettings, PrinterSettingsInfo printerSettings) printSettings = await GetPrintSettingsAsync();

            if (printSettings == default)
            {
                return;
            }

            ReturnInvoiceSupplierReportData data = new ReturnInvoiceSupplierReportData(
                Model.Products
                    .Where(x => x.AcceptQuantity > 0)
                    .GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(x => new ReturnInvoiceSupplierReportCategoryData(
                        x.Key.CategoryName,
                        x.Select(z => new ReturnInvoiceSupplierReportProductData(
                            z.ProductId,
                            z.FullName,
                            z.AcceptQuantity ?? 0)).ToList())).ToList(),
                Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId)?.Name,
                Carries.FirstOrDefault(x => x.Id == Model.CarryId)?.Name,
                Model.SupplierName,
                Model.Id,
                Model.CreatedOn.ToString("dd.MM.yyyy"));

            IReport report = new ReturnInvoiceSupplierReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, copies == 1, printSettings.printerSettings.Name, printSettings.printerSettings.PaperSource, copies);

            await Mediator.Send(request);
        }

        private async Task SwitchDocumentsControlAsync()
        {
            const string errorRu = "Ошибка при смене контроля документов";
            const string errorEn = "Error while switching documents control";

            try
            {
                Result<ReturnInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new SwitchDocumentsControlReturnInvoice(Model.Id));
                Messenger.Send(new ReturnInvoiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Контроль документов успешно изменен");

                SetData(result.Data);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                ShowValidationResultView(errorRu, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, errorEn);
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(errorRu);
                Logger.LogError(exception, errorEn);
            }
        }

        private async Task PrintTrackNumberAsync()
        {
            if (!string.IsNullOrEmpty(Model.TrackNumber))
            {
                await _trackNumberProvider.PrintAsync(Model.TrackNumber, true);
            }
        }

        private async Task SetSenderNpContractorAsync()
        {
            GetNpContractorViewModel npContractorViewModel = DialogDocumentManagerService.ShowView<GetNpContractorViewModel>(new GetNpContractorParameter(Model.SenderNpContractorRef), this);

            if (npContractorViewModel.IsOk)
            {
                (await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new SetReturnInvoiceSenderNpContractor(Model.Id, npContractorViewModel.SelectedNpContractorRef)),
                    "изменении отправителе НП",
                    "Отправитель НП изменен",
                    this,
                    true))
                .IfNotNull(x =>
                {
                    Messenger.Send(new ReturnInvoiceMessage(x.Data, MessageType.Changed));
                    SetData(x.Data);
                });
            }
        }

        private void OnDocumentMessage(ReturnInvoiceDocumentMessage message)
        {
            if (message.Entity.ReturnInvoiceId == Model.Id)
            {
                switch (message.MessageType)
                {
                    case MessageType.Added:
                        Documents?.Add(message.Entity);
                        Model.DocumentsCount = Documents?.Count;
                        break;
                }
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            WarehouseDto warehouse = Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId);
            ReturnInvoiceState state = States.FirstOrDefault(x => x.Id == Model.StateId);
            CarryDto carry = Carries.FirstOrDefault(x => x.Id == Model.CarryId);
            EmployeeDto employee = Employees.FirstOrDefault(x => x.Id == Model.CreatedBy);

            yield return new SummaryViewItem("Возврат", Model.Id.ToString());

            if (warehouse != null)
            {
                yield return new SummaryViewItem("Склад", warehouse.Name);
            }

            if (state != null)
            {
                yield return new SummaryViewItem("Статус", state.Name);
            }

            if (employee != null)
            {
                yield return new SummaryViewItem("Создал", employee.Name);
            }

            yield return new SummaryViewItem("Создано", Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat));

            if (carry != null)
            {
                yield return new SummaryViewItem("Доставка", carry.Name);
            }

            yield return new SummaryViewItem("Контроль документов", Model.DocumentsControl.ToStringAlt());
            yield return new SummaryViewItem("Отправитель НП", _npContractorNames.GetValueOrDefault(Model.SenderNpContractorRef ?? string.Empty, "-Не задан-"));

            if (!string.IsNullOrWhiteSpace(Model.TrackNumber))
            {
                yield return new SummaryViewItem("ТТН", Model.TrackNumber);
            }
        }

        private async Task<(PrintingSettingsInfo printingSettings, PrinterSettingsInfo printerSettings)> GetPrintSettingsAsync()
        {
            (PrintingSettingsInfo printingSettings, PrinterSettingsInfo printerSettings) settings;

            settings.printingSettings = await PrintingSettingsStore.LoadAsync();

            settings.printerSettings = settings.printingSettings.Main;

            if (settings.printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return default;
            }

            return settings;
        }

        private void ShowDocumentsBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter(DocumentsBotHelper.GetUrl(Model.Id, Entity.ReturnInvoiceId, Dictionaries, _telegramBotOptions), "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }
    }
}