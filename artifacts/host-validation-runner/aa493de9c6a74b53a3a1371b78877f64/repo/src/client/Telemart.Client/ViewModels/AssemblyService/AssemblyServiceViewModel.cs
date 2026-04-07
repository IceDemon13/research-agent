using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.ReportDesigner.Order;
using Telemart.Client.Reports.AssemblyService;
using Telemart.Client.Reports.Order.Assembly;
using Telemart.Client.Reports.Product;
using Telemart.Client.Reports.ReportBuilders.AssemblyService.PassportReport;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Assembly;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public sealed class AssemblyServiceViewModel : TelemartEditorViewModelBase<AssemblyServiceDto, AssemblyServiceParameter, AssemblyServiceViewItem>
    {
        private IReadOnlyDictionary<int, List<string>> _accountingSystemSerials;
        private ScanSerialMode _scanSerialMode = ScanSerialMode.Single;
        private IReadOnlyDictionary<int, List<string>> _parentProductSerials;
        private Dictionary<int, List<string>> _scanParentProductSerials;

        public AssemblyServiceViewModel(
           IWebClient webClient,
           IMessageFacadeService messageFacadeService,
           IMessenger messenger,
           IMapper mapper,
           IMediator mediator,
           IDictionaries dictionaries,
           IAssemblyServicePassportReportPrinter passportReportPrinter,
           IPrintingSettingsStore printingSettingsStore,
           DocumentCommands documentCommands,
           ProductInformationViewModel productInformationViewModel,
           IBarcodeReportFactory barcodeReportFactory,
           IErrorHandler errorHandler)
           : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;
            ErrorHandler = errorHandler;

            StartAssemblyCommand = new AsyncCommand(StartAssemblyAsync, () => Model?.StateId == AssemblyServiceState.Warehouse.Id && Model?.EmployeeLockId == null && Model?.ParentAssemblyServiceId.HasValue != true);
            StartTestingCommand = new AsyncCommand(StartTestingAsync, () => Model?.StateId == AssemblyServiceState.Assembled.Id && Model?.EmployeeLockId == null);

            TestsCommand = new DelegateCommand(OpenTests, () => Model?.StateId == AssemblyServiceState.Testing.Id && Model?.EmployeeLockId == null);
            TestResultsCommand = new DelegateCommand(OpenTestResults, () => Model?.StateId == AssemblyServiceState.Completed.Id && Model?.ParentAssemblyServiceId.HasValue != true);

            CompleteAssemblyCommand = new AsyncCommand(CompleteAssemblyAsync, () => Model?.StateId == AssemblyServiceState.Testing.Id && Model?.EmployeeLockId == null);
            AssembleAssemblyCommand = new AsyncCommand(AssembleAssemblyAsync, () => Model?.StateId == AssemblyServiceState.Assembling.Id && Model?.EmployeeLockId == null);
            DisassembledAssemblyCommand = new AsyncCommand(DisassembledAssemblyAsync, CanDisassembledAssembly);
            StartDisassemblyAssemblyCommand = new AsyncCommand(StartDisassemblyAssemblyAsync, CanStartDisassemblyAssembly);

            StopAssemblyCommand = new AsyncCommand(StopAssemblyAsync, () => Model?.StateId == AssemblyServiceState.Assembling.Id && Model?.EmployeeLockId == null);
            PrintAssemblyCommand = new AsyncCommand(PrintAssemblyAsync, () => Model?.EmployeeLockId == null);
            PrintBarcodeMovementCommand = new AsyncCommand(PrintBarcodeMovementAsync, () => Model != null && Model.EmployeeLockId == null && ((Model.Places.HasValue && Model.CompletedOn.HasValue) || Model.ParentAssemblyServiceId.HasValue));
            PrintOurBarcodeCommand = new AsyncCommand(PrintOurBarcodeAsync, () => Model != null && Model.EmployeeLockId == null && Model.ProductId.HasValue && Model?.StateId == AssemblyServiceState.Completed.Id);

            OpenOrderCommand = new DelegateCommand<int?>(OpenOrder, x => x.HasValue);
            EditProductSerialsCommand = new DelegateCommand<AssemblyServiceProductViewItem>(EditProductSerials, x => IsLockedByCurrentEmployee || x?.SerialNumbers?.Any() == true);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickInfo>(HandleRowDoubleClick, x => x != null);
            AdditionalServiceProductsOpenCommand = new DelegateCommand<AdditionalServiceProductsViewItem>(AdditionalServiceProductsOpen, _ => SelectedAdditionalProduct != null);
            PrintAssemblyPassportCommand = new AsyncCommand<bool>(PrintAssemblyServicePassportAsync, _ => Model?.StateId == AssemblyServiceState.Completed.Id && Model.ProductId.HasValue);
            PrintAssemblySheetCommand = new AsyncCommand(PrintAssemblySheetAsync, () => Model != null);
            OpenParentAssemblyServiceCommand = new DelegateCommand<int?>(OpenParentAssemblyService, x => x.HasValue);

            PrintB2BBarcodeCommand = new AsyncCommand<short>(PrintB2BBarcodeAsync, canExecuteMethod: _ => Model != null && Model.EmployeeLockId == null && Model.ProductId.HasValue && Model.StateId == AssemblyServiceState.Completed.Id);
            PrintB2BSerialCommand = new AsyncCommand(PrintB2BSerialAsync, () => Model != null && Model.EmployeeLockId == null && Model.ProductId.HasValue && Model.StateId == AssemblyServiceState.Completed.Id);

            ClearScannedQuantityCommand = new DelegateCommand<AssemblyServiceProductViewItem>(ClearScannedQuantity, _ => IsLockedByCurrentEmployee);

            Warehouses = new ObservableRangeCollection<WarehouseDto>();

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;

            AllowPrintAssemblyReport = WebClient.IsOperationAllowed(BusinessOperation.AssemblyServicePrint);

            Mediator = mediator;
            PrintingSettingsStore = printingSettingsStore;
            PassportReportPrinter = passportReportPrinter;
            BarcodeReportFactory = barcodeReportFactory;
            ProductInformation = productInformationViewModel;
        }

        public AssemblyServiceViewModel()
        {
        }

        public IAsyncCommand StartAssemblyCommand { get; }

        public IAsyncCommand PrintAssemblyCommand { get; }

        public IAsyncCommand PrintBarcodeMovementCommand { get; }

        public IAsyncCommand PrintOurBarcodeCommand { get; }

        public IAsyncCommand CompleteAssemblyCommand { get; }

        public IAsyncCommand AssembleAssemblyCommand { get; }

        public IAsyncCommand DisassembledAssemblyCommand { get; }

        public IAsyncCommand StartDisassemblyAssemblyCommand { get; }

        public IDelegateCommand ClearScannedQuantityCommand { get; }

        public IAsyncCommand PrintAssemblyPassportCommand { get; }

        public IAsyncCommand PrintB2BBarcodeCommand { get; }

        public IAsyncCommand PrintB2BSerialCommand { get; }

        public IAsyncCommand PrintAssemblySheetCommand { get; }

        public IAsyncCommand StopAssemblyCommand { get; }

        public IAsyncCommand StartTestingCommand { get; }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        public IDelegateCommand OpenOrderCommand { get; }

        public IDelegateCommand OpenParentAssemblyServiceCommand { get; }

        public IDelegateCommand EditProductSerialsCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand AdditionalServiceProductsOpenCommand { get; }

        public IDelegateCommand TestResultsCommand { get; }

        public IDelegateCommand TestsCommand { get; }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            set { SetProperty(() => ProductInformation, value); }
        }

        public AssemblyServiceProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, RefreshValues); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public bool AllowPrintAssemblyReport
        {
            get { return GetProperty(() => AllowPrintAssemblyReport); }
            private set { SetProperty(() => AllowPrintAssemblyReport, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductsViewItem> AdditionalServiceProducts
        {
            get { return GetProperty(() => AdditionalServiceProducts); }
            set { SetProperty(() => AdditionalServiceProducts, value, () => RaisePropertyChanged(nameof(VisibleAdditionalServicesGrid))); }
        }

        public AdditionalServiceProductsViewItem SelectedAdditionalProduct
        {
            get { return GetProperty(() => SelectedAdditionalProduct); }
            set { SetProperty(() => SelectedAdditionalProduct, value); }
        }

        #region Collections

        public IEnumerable<SummaryViewItem> AssemblySummaryItems
        {
            get { return GetProperty(() => AssemblySummaryItems); }
            private set { SetProperty(() => AssemblySummaryItems, value); }
        }

        public ReadOnlyObservableCollection<AssemblyServiceState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServiceProductState> AdditionalStates
        {
            get { return GetProperty(() => AdditionalStates); }
            set { SetProperty(() => AdditionalStates, value); }
        }

        public ReadOnlyObservableCollection<AdditionalServicePriorityType> PriorityTypes
        {
            get { return GetProperty(() => PriorityTypes); }
            set { SetProperty(() => PriorityTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableRangeCollection<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<CategoryType> CategoryTypes
        {
            get { return GetProperty(() => CategoryTypes); }
            private set { SetProperty(() => CategoryTypes, value); }
        }

        public int CategoryTypeGroupIndex
        {
            get { return GetProperty(() => CategoryTypeGroupIndex); }
            set { SetProperty(() => CategoryTypeGroupIndex, value); }
        }

        #endregion

        #region DialogSettings

        public override int Width => 1280;

        public override int MinWidth => 750;

        public override int MaxWidth => 1920;

        public override int Height => 720;

        public override int MinHeight => 576;

        public override int MaxHeight => 1080;

        #endregion

        public bool VisibleAdditionalServicesGrid => AdditionalServiceProducts?.Any() == true;

        protected override string EntityName => "Сборка";

        protected override string UpdatedActionMessage => "сохранена";

        protected override string CreatedActionMessage => string.Empty;

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IAssemblyServicePassportReportPrinter PassportReportPrinter { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private DocumentCommands DocumentCommands { get; }

        private IErrorHandler ErrorHandler { get; }

        protected override bool CanEdit()
        {
            return Model?.StateId != AssemblyServiceState.Assembled.Id
                && Model?.StateId != AssemblyServiceState.Testing.Id
                && Model?.StateId != AssemblyServiceState.Completed.Id;
        }

        protected override object CreateEntityMessage(AssemblyServiceDto dto, MessageType messageType)
        {
            return new AssemblyServiceMessage(dto, messageType);
        }

        protected override Task<AssemblyServiceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(id));
        }

        protected override Task<LockResponse<AssemblyServiceDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAssemblyService(id));
        }

        protected override Task<LockResponse<AssemblyServiceDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAssemblyService(id));
        }

        protected override void SetEditTitle()
        {
            Title = $"Сборка ({Model.Id})";
        }

        protected override Task<Result<AssemblyServiceDto>> UpdateEntityAsync()
        {
            AssemblyServiceProductSaveDto[] productSaveDtos = Model.Products
                .Select(x => new AssemblyServiceProductSaveDto(x.Id, x.ScannedQuantity, x.SerialNumbers))
                .ToArray();

            return WebClient.ExecuteApiRequestAsync(new UpdateAssemblyService(Model.Id, Model.EmployeeId, productSaveDtos));
        }

        protected override Task<Result<AssemblyServiceDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override async Task HandleLoadedAsync()
        {
            States = Dictionaries.GetItems<AssemblyServiceState>().ToReadOnlyObservableCollection();

            CategoryTypes = Dictionaries.GetItems<CategoryType>().ToReadOnlyObservableCollection();

            AdditionalStates = Dictionaries.GetItems<AdditionalServiceProductState>().ToReadOnlyObservableCollection();

            PriorityTypes = Dictionaries.GetItems<AdditionalServicePriorityType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshEmployeesAsync(), LoadWarehousesAsync());

            await base.HandleLoadedAsync();

            AdditionalServiceProducts = Model.AdditionalServices.Select(x => Mapper.Map<AdditionalServiceProductsViewItem>(x)).ToReadOnlyObservableCollection();

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

            await LoadParentAssemblyServiceAsync();
        }

        protected override void AfterSetData()
        {
            AssemblySummaryItems = GetSummaryItems();

            CategoryTypeGroupIndex = 0;
        }

        private async Task LoadWarehousesAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses.AddRange(warehouses.Data);
        }

        private void RefreshValues()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, Currency.UahId);
            }
        }

        private async Task LoadParentAssemblyServiceAsync()
        {
            if (Model.ParentAssemblyServiceId.HasValue)
            {
                AssemblyServiceDto parentAssemblyService = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(Model.ParentAssemblyServiceId));

                _parentProductSerials = parentAssemblyService.Products.Where(x => x.SerialNumbers?.Any() == true)
                    .ToDictionary(x => x.ProductId, x => x.SerialNumbers);
            }
        }

        private void EditProductSerials(AssemblyServiceProductViewItem productViewItem)
        {
            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(productViewItem.SerialNumbers, !IsLockedByCurrentEmployee), this);

            if (!IsLockedByCurrentEmployee)
            {
                return;
            }

            if (viewModel.IsOk)
            {
                if (viewModel.SerialNumbers.Count != productViewItem.SerialNumbers.Count)
                {
                    productViewItem.ScannedQuantity = viewModel.SerialNumbers.Count;
                }

                productViewItem.SerialNumbers = viewModel.SerialNumbers.ToList();
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickInfo e)
        {
            if (string.Equals(e.FieldName, nameof(AssemblyServiceProductViewItem.ScannedQuantity), StringComparison.Ordinal))
            {
                EditProductSerialsCommand.Execute((AssemblyServiceProductViewItem)e.Data);
            }
        }

        private void AdditionalServiceProductsOpen(AdditionalServiceProductsViewItem item)
        {
            Messenger.Send(new AdditionalServiceProductViewMessage(item.Id));
        }

        private void ClearScannedQuantity(AssemblyServiceProductViewItem item)
        {
            item.ScannedQuantity = 0;
        }

        private void OpenOrder(int? orderId)
        {
            Messenger.Send(new OrderEditViewMessage(orderId!.Value));
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            IsRecognitionInProgress = false;

            if (Model.ParentAssemblyServiceId.HasValue && Model?.StartDisassemblyBy.HasValue != true)
            {
                e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Разборка не начата");

                return;
            }

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
            AssemblyServiceProductViewItem[] productViewItems = Model.Products.Where(x => x.ProductId == product.ProductId).ToArray();

            if (!productViewItems.Any())
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товара {product.FullName} нет в заказе");
            }

            if (productViewItems.All(x => x.ScannedQuantity >= x.Quantity))
            {
                return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, $"Товар {product.FullName} уже просканирован");
            }

            AssemblyServiceProductViewItem productViewItem = productViewItems.First(x => x.ScannedQuantity < x.Quantity);

            if (productViewItem.KeepSerialOverridden)
            {
                string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productViewItem.ProductId).ToArray();

                ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                    productViewItem.ProductId,
                    productViewItem.SerialNumbers,
                    barcodes,
                    product.SerialNumberLength,
                    _scanSerialMode,
                    _accountingSystemSerials[productViewItem.ProductId]);

                ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    if (Model.ParentAssemblyServiceId.HasValue)
                    {
                        RecognizeBarcodeMessage message = CheckParentSerials(viewModel);

                        if (message != null)
                        {
                            return message;
                        }
                    }

                    productViewItem.SerialNumbers.AddRange(viewModel.SerialNumbers);
                    productViewItem.ScannedQuantity = productViewItem.SerialNumbers.Count;

                    _scanSerialMode = viewModel.ScanMode;
                }
            }
            else
            {
                productViewItem.ScannedQuantity += quantity;
            }

            SelectedProduct = productViewItem;

            Model.Products.FirstOrDefault(x => x.Id == SelectedProduct.Id)!.ScannedQuantity = SelectedProduct.ScannedQuantity;

            return null;

            RecognizeBarcodeMessage CheckParentSerials(ProductScanSerialsViewModel view)
            {
                _scanParentProductSerials ??= new Dictionary<int, List<string>>();

                if (_scanParentProductSerials.TryGetValue(productViewItem.ProductId, out List<string> _))
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товара с таким SN уже просканирован");
                }

                if (_parentProductSerials?.TryGetValue(productViewItem.ProductId, out List<string> parentsSerials) == true)
                {
                    if (view.SerialNumbers.Except(parentsSerials).Any())
                    {
                        return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товара с таким SN нет в зборке, которую разбирают");
                    }

                    _scanParentProductSerials.Remove(productViewItem.ProductId);
                    _scanParentProductSerials.TryAdd(productViewItem.ProductId, view.SerialNumbers.ToList());
                }
                else
                {
                    return new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товара с таким SN нет в зборке, которую разбирают");
                }

                return null;
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task StartTestingAsync()
        {
            try
            {
                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new StartTestingAssemblyService(Model.Id));
                SetData(result.Data);

                AssemblySummaryItems = GetSummaryItems();
                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Тестирование успешно начато");
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to start test");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to start test");
                MessageFacadeService.ShowNotificationError("Ошибка начала тестирования");
            }
        }

        private void OpenTests()
        {
            LockEntityAsync(Model.Id);
            SizeableDialogDocumentManagerService.ShowView<AssemblyServiceTestViewModel>(new AssemblyServiceTestParameter(Model.Id, true), this);
            UnlockEntityAsync(Model.Id);
        }

        private void OpenTestResults()
        {
            SizeableDialogDocumentManagerService.ShowView<AssemblyServiceTestViewModel>(new AssemblyServiceTestParameter(Model.Id, false), this);
        }

        private async Task StartAssemblyAsync()
        {
            try
            {
                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new StartAssemblyService(Model.Id));
                SetData(result.Data);

                AssemblySummaryItems = GetSummaryItems();
                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Сборка успешно начата");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при старте сборки");
                ShowValidationResultView("Ошибка при старте сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to start assembly");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при старте сборки");
                Logger.LogError(exception, "Error while starting assembly");
            }
        }

        private async Task PrintBarcodeMovementAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            IReport report;

            if (Model.ProductId.HasValue)
            {
                AssembledComputerRuleMovementReportData[] reportsData = new AssembledComputerRuleMovementReportData[Model.Places!.Value];

                for (int i = 0; i < Model.Places; i++)
                {
                    reportsData[i] = new AssembledComputerRuleMovementReportData(
                    Model.OrderId,
                    Model.Id,
                    Model.Places.Value,
                    i + 1,
                    Model.Products.Count(x => x.ProductId != Constants.AssemblyServiceProductId),
                    Model.NomenclatureSeries,
                    Model.ProductName,
                    Model.ProductId.Value);
                }

                report = new AssembledComputerRuleMovementReport { DataSource = reportsData };
            }
            else
            {
                AssemblyServiceMovementReportData[] reportsData = new AssemblyServiceMovementReportData[Model.Places!.Value];

                for (int i = 0; i < Model.Places; i++)
                {
                    reportsData[i] = new AssemblyServiceMovementReportData(
                    Model.OrderId,
                    i + 1,
                    Model.Places.Value,
                    Model.Products.Count(x => x.ProductId != Constants.AssemblyServiceProductId),
                    Model.Id);
                }

                report = new AssemblyServiceMovementReport { DataSource = reportsData };
            }

            PrintReportRequest printRequest = printSettings.Sticker != null
                    ? new PrintReportRequest(report, true, printSettings.Sticker.Name, printSettings.Sticker.PaperSource)
                    : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintOurBarcodeAsync()
        {
            const int quantity = 1;

            if (Model.ProductId.HasValue)
            {
                ProductCardDto productCard = await WebClient.ExecuteApiRequestAsync(new QueryProductCard(Model.ProductId.Value));

                BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(productCard.NameUkr ?? productCard.Name, productCard.ProductId, quantity);

                if (result.Printer == null)
                {
                    MessageFacadeService.ShowNotificationError("Для печати \"Нашего ШК\" нужно задать принтер в настройках");
                    return;
                }

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    result.Report,
                    false,
                    result.Printer.Name,
                    result.Printer.PaperSource);

                await Mediator.Send(printReportRequest);
            }
        }

        private async Task PrintAssemblyServicePassportAsync(bool showPreview)
        {
            await PassportReportPrinter.PrintAsync(new AssemblyServicePassportReportPrinterData(Model.Id, showPreview));
        }

        private async Task PrintAssemblySheetAsync()
        {
            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            AssemblySheetReportDataDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryAssemblySheetReport(Model.Id));

            OrderSingleAssemblyReportData reportData = Mapper.Map<OrderSingleAssemblyReportData>(reportDto);

            IReadOnlyCollection<AdditionalServiceProductDto> additionalServiceProducts = Model.AdditionalServices;

            int[] parentOrderProductIds = additionalServiceProducts.Where(x => x.ParentOrderProductId.HasValue).Select(x => x.ParentOrderProductId.Value).ToArray();

            reportData.AssemblyServiceProducts = reportDto.AssemblyServiceProducts
                .Where(x => x.OrderProductId == null || parentOrderProductIds.Contains(x.OrderProductId.Value) != true)
                .Select(x => Mapper.Map<OrderAssemblyProductReportData>(x)).ToArray();

            AdditionalStates = Dictionaries.GetItems<AdditionalServiceProductState>().ToReadOnlyObservableCollection();

            PriorityTypes = Dictionaries.GetItems<AdditionalServicePriorityType>().ToReadOnlyObservableCollection();

            reportData.SetDateTimeAssemblyPrint(DateTime.Now);

            if (additionalServiceProducts.Any())
            {
                List<AssemblyAdditionalServiceProductData> assemblyAdditionalServiceProducts = new List<AssemblyAdditionalServiceProductData>();

                foreach (AdditionalServiceProductDto additionalServiceProduct in additionalServiceProducts)
                {
                    string consumableProducts = additionalServiceProduct.ConsumableProducts?.Any() == true
                        ? string.Join($",{Environment.NewLine}", additionalServiceProduct.ConsumableProducts.Select(x => x.ProductName))
                        : string.Empty;

                    assemblyAdditionalServiceProducts.Add(
                        new AssemblyAdditionalServiceProductData(
                            additionalServiceProduct.Id,
                            string.IsNullOrEmpty(consumableProducts) ? additionalServiceProduct.AdditionalServiceNameRu : $"{additionalServiceProduct.AdditionalServiceNameRu}{Environment.NewLine}Товар для оказания услуги: {consumableProducts}",
                            additionalServiceProduct.ProductName,
                            Dictionaries.GetItemById<AdditionalServiceProductState>(additionalServiceProduct.PriorityTypeId).Name,
                            Dictionaries.GetItemById<AdditionalServicePriorityType>(additionalServiceProduct.PriorityTypeId).Name,
                            OrderCommentHelper.GetJoinedComment(additionalServiceProduct.CustomerComment, additionalServiceProduct.EmployeeComment, additionalServiceProduct.SystemComment)));
                }

                reportData.SetAssemblyAdditionalServiceProducts(assemblyAdditionalServiceProducts);
            }

            IReport report = new OrderSingleAssemblyReport { DataSource = new[] { reportData } };

            PrintReportRequest printRequest = printSettings?.Main != null
                ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                : new PrintReportRequest(report, true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintAssemblyAsync()
        {
            try
            {
                List<AssemblyServiceBarcodeReportData> barcodeReportData = new List<AssemblyServiceBarcodeReportData>();

                foreach (AssemblyServiceProductViewItem product in Model.Products)
                {
                    for (int i = 0; i < product.Quantity; i++)
                    {
                        barcodeReportData.Add(new AssemblyServiceBarcodeReportData(product.SerialNumbers.Skip(i).FirstOrDefault(), product.FullName, product.ProductId, product.Quantity));
                    }
                }

                AssemblyServiceReportData reportData = new AssemblyServiceReportData(WebClient.AuthenticatedEmployee.Name, Model.OrderId, Model.Id, barcodeReportData);

                IReport report = new AssemblySerialsReport { DataSource = new[] { reportData } };

                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                PrintReportRequest printRequest = printSettings?.Main != null
                    ? new PrintReportRequest(report, true, printSettings.Main.Name, printSettings.Main.PaperSource)
                    : new PrintReportRequest(report, true);

                await Mediator.Send(printRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print assembly service");
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private async Task PrintB2BBarcodeAsync(short copies = 2)
        {
            BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(BarcodeReportFormat.B2B, Model.ProductName, Model.ProductId.Value, 1);

            if (result.Printer == null)
            {
                MessageFacadeService.ShowNotificationError("Для печати \"ШК B2B\" нужно задать принтер 50x40 в настройках");
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

        private async Task PrintB2BSerialAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Barcode50X40;

            if (printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Для печати \"SN B2B\" нужно задать принтер 50x40 в настройках");
                return;
            }

            IReport report = new B2BSerialNumberReport
            {
                DataSource = new List<SerialNumberReportData>
                    {
                        new SerialNumberReportData(Model.NomenclatureSeries)
                    }
            };

            PrintReportRequest request = new PrintReportRequest(
                report,
                false,
                printerSettings.Name,
                printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task StopAssemblyAsync()
        {
            try
            {
                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new StopAssemblyService(Model.Id));
                SetData(result.Data);

                AssemblySummaryItems = GetSummaryItems();
                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Сборка успешно остановлена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при остановке сборки");
                ShowValidationResultView("Ошибка при остановке сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to stop assembly");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при остановке сборки");
                Logger.LogError(exception, "Error while stopping assembly");
            }
        }

        private void OpenParentAssemblyService(int? parentAssemblyServiceId)
        {
            Messenger.Send(new AssemblyServiceViewMessage(parentAssemblyServiceId!.Value));
        }

        private async Task AssembleAssemblyAsync()
        {
            try
            {
                GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Количество мест сборки",
                "Количество мест сборки",
                @"^\d+$",
                "Количество мест должно быть числом");

                GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

                if (!viewModel.IsOk || viewModel.Content is null)
                {
                    return;
                }

                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new AssembleAssemblyService(Model.Id, Convert.ToInt32(viewModel.Content)));
                SetData(result.Data);

                AssemblySummaryItems = GetSummaryItems();
                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Сборка успешно завершена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении сборки");
                ShowValidationResultView("Ошибка при завершении сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to complete assembly");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении сборки");
                Logger.LogError(exception, "Error while completing assembly");
            }
        }

        private async Task DisassembledAssemblyAsync()
        {
            Result<AssemblyServiceDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DisassembleAssemblyService(Model.Id)),
                "завершении разборки",
                "Cборка разобрана",
                this,
                true);

            SetData(result.Data);

            AssemblySummaryItems = GetSummaryItems();
            Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
        }

        private bool CanDisassembledAssembly()
        {
            return Model?.StateId == AssemblyServiceState.Disassembling.Id
                   && Model?.EmployeeLockId == null
                   && Model?.ParentAssemblyServiceId.HasValue == true
                   && Model?.StartDisassemblyBy.HasValue == true;
        }

        private async Task StartDisassemblyAssemblyAsync()
        {
            Result<AssemblyServiceDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new DisassemblyStartAssemblyService(Model.Id)),
                "старте разборки",
                "Разборка начата",
                this,
                true);

            SetData(result.Data);

            AssemblySummaryItems = GetSummaryItems();
            Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
        }

        private bool CanStartDisassemblyAssembly()
        {
            return Model?.StateId == AssemblyServiceState.Warehouse.Id
                   && Model?.EmployeeLockId == null
                   && Model?.ParentAssemblyServiceId.HasValue == true
                   && Model?.StartDisassemblyBy.HasValue != true;
        }

        private async Task CompleteAssemblyAsync()
        {
            try
            {
                Result<AssemblyServiceDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteAssemblyService(Model.Id));

                SetData(result.Data);

                await PrintBarcodeMovementAsync();

                Messenger.Send(new AssemblyServiceMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Тестирование успешно завершено");

                Close();

                PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

                if (Model.ProductId.HasValue)
                {
                    await PrintAssemblyServicePassportAsync(false);

                    if (printingSettings?.PrintBarcodeAssemblyProduct == true)
                    {
                        await PrintB2BBarcodeAsync();
                        await PrintB2BSerialAsync();
                    }
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении тестирования сборки");
                ShowValidationResultView("Ошибка при завершении тестирования сборки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to finish testing assembly");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершения тестирования сборки");
                Logger.LogError(exception, "Error while finishing testing assembly");
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            WarehouseDto warehouse = Warehouses.FirstOrDefault(x => x.Id == Model.WarehouseId);
            AssemblyServiceState state = States.FirstOrDefault(x => x.Id == Model.StateId);

            yield return new SummaryViewItem("Вид работ", Model.KindOfJob, Model.ParentAssemblyServiceId.HasValue ? SummaryViewItem.RedLevel : SummaryViewItem.NormalLevel);

            yield return new SummaryViewItem("Номер", Model.Id.ToString());
            yield return new SummaryViewItem("Склад", warehouse?.Name);
            yield return new SummaryViewItem("Статус", state?.Name);

            if (Model.ArrivedOn.HasValue)
            {
                yield return new SummaryViewItem("Дата прибытия", Model.ArrivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            yield return new SummaryViewItem("Дата сборки (план)", Model.AssemblyDate.ToString(DateFormattingRules.DateFormat));

            if (Model.CompletedOn.HasValue)
            {
                yield return new SummaryViewItem("Дата сборки (факт)", Model.CompletedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (Model.OrderDeliveryTimeTo.HasValue)
            {
                yield return new SummaryViewItem("Дата X", Model.OrderDeliveryTimeTo.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (Model.Places.HasValue)
            {
                yield return new SummaryViewItem("Кол-во мест", Model.Places.Value.ToString());
            }

            if (Model.StateId == AssemblyServiceState.Completed.Id && Model.StartTestBy != null && Model.StartTestOn != null)
            {
                yield return new SummaryViewItem("Начал тест", Employees.FirstOrDefault(x => x.Id == Model.StartTestBy).DisplayValue);
                yield return new SummaryViewItem("Начало тест", Model.StartTestOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (Model.StartDisassemblyBy.HasValue)
            {
                yield return new SummaryViewItem("Начал разборку", Employees.FirstOrDefault(x => x.Id == Model.StartDisassemblyBy).DisplayValue);
                yield return new SummaryViewItem("Начало разборки", Model.StartDisassemblyOn!.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }
        }
    }
}