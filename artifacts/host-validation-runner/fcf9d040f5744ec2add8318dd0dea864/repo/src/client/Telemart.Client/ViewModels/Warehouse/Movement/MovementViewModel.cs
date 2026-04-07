using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Business.Delivery.TrackNumberProviders;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Movement.Actions;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.Teks;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.ReportDesigner;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.MovementReport;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Teks;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    internal sealed class MovementViewModel : TelemartEditorViewModelBase<MovementDto, MovementViewMessage, MovementViewItem>
    {
        private const string OtherConfigurationStr = "🖥️ Остальные конфигурации ПК";
        private const string ResetQuantityConfirmMsg = "Очистить текущее значение?";

        private ScanSerialMode scanSerialMode = ScanSerialMode.Single;
        private IReadOnlyDictionary<int, List<string>> accountingSystemSerials;
        private IReadOnlyDictionary<int, string> employees;
        private Dictionary<int, int> productParentCategories;
        private IReadOnlyDictionary<int, CategoryDto> categories;
        private IReadOnlyCollection<MovementAdditionalServiceProductDto> additionalServiceProducts;
        private List<ProductAttributesDto> productAttributes;
        private IReadOnlyCollection<(int productId, string nomenclatureSeries)> movementAssembledComputersNomenclatureSeries;
        private ISet<string> _anotherOrdersNomenclatureSeries;
        private IReadOnlyDictionary<string, int> _nomenclatureSeriesWithOrders;
        private WarehouseDto _warehouseFrom;

        public MovementViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IMediator mediator,
            IErrorHandler errorHandler,
            IPriceConverterFactory priceConverterFactory,
            IBarcodeReportFactory barcodeReportFactory,
            IPrintingSettingsStore printingSettingsStore,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            SerializeMovementAndSaveToLogsCommand = new DelegateCommand(SerializeMovementAndSaveToLogs);
            ExportToExcelCommand = new DelegateCommand<TableView>(ExportToExcel);
            SendMovementCommand = new AsyncCommand(SendMovementAsync);
            FastSendMovementCommand = new AsyncCommand(FastSendMovementAsync);
            ReceiveMovementCommand = new AsyncCommand(ReceiveMovementAsync);
            ArriveMovementCommand = new AsyncCommand(ArriveMovementAsync);
            ResetQuantityOutCommand = new DelegateCommand<MovementProductViewItem>(ResetQuantityOut, x => x != null);
            ResetQuantityInCommand = new DelegateCommand<MovementProductViewItem>(ResetQuantityIn, x => x != null);
            PrintOurBarcodeCommand = new AsyncCommand<MovementProductViewItem>(PrintOurBarcodeAsync, x => x != null);
            PrintCommand = new AsyncCommand(PrintAsync);
            PrintTtnCommand = new AsyncCommand(PrintTttAsync, () => Model is not null && (Model.CarryId == CarryType.NpDeliveryId || Model.CarryId == CarryType.NpWarehouseId || Model.CarryId == CarryType.TeksId) && !string.IsNullOrWhiteSpace(Model.TrackNumber));
            PrintInvoiceTtnCommand = new AsyncCommand(PrintInvoiceTttAsync, () => Model?.CarryId == CarryType.TeksId && !string.IsNullOrEmpty(Model?.TrackNumber));
            PrintSendingCommand = new AsyncCommand(PrintSendingAsync, () => Model is not null && (Model.State == MovementState.Arrived || Model.State == MovementState.Left || Model.State == MovementState.Received));
            CancelMovementCommand = new AsyncCommand(CancelMovementAsync, () => Model?.State.Id == MovementState.New.Id && WebClient.IsOperationAllowed(BusinessOperation.MovementCancel) && Model?.EmployeeLockId is null);
            AddProductCommand = new DelegateCommand(AddProduct, () => IsLockedByCurrentEmployee && Model?.State == MovementState.New);
            DeleteProductCommand = new DelegateCommand<MovementProductViewItem>(DeleteProduct, CanDelete);
            HandleRowDoubleClickCommand = new DelegateCommand(HandleRowDoubleClick);

            ShowQuantityInSerialsCommand = new DelegateCommand<MovementProductViewItem>(ShowQuantityInSerials, _ => Model?.EmployeeLockId.HasValue == true);
            ShowQuantityOutSerialsCommand = new DelegateCommand<MovementProductViewItem>(ShowQuantityOutSerials, _ => Model?.EmployeeLockId.HasValue == true);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;

            Mediator = mediator;
            BarcodeReportFactory = barcodeReportFactory;
            PrintingSettingsStore = printingSettingsStore;
            ErrorHandler = errorHandler;
            PriceConverterFactory = priceConverterFactory;
            ProductInformation = productInformationViewModel;

            AllowAddOrDeleteProduct = WebClient.IsOperationAllowed(BusinessOperation.MovementAddDeleteProduct);
            AutoUnlock = false;
            additionalServiceProducts = new List<MovementAdditionalServiceProductDto>();
        }

        public MovementViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand SerializeMovementAndSaveToLogsCommand { get; }

        public IDelegateCommand ExportToExcelCommand { get; }

        public IAsyncCommand SendMovementCommand { get; }

        public IAsyncCommand FastSendMovementCommand { get; }

        public IAsyncCommand ReceiveMovementCommand { get; }

        public IAsyncCommand ArriveMovementCommand { get; }

        public IDelegateCommand ResetQuantityOutCommand { get; }

        public IDelegateCommand ResetQuantityInCommand { get; }

        public IAsyncCommand PrintOurBarcodeCommand { get; }

        public IAsyncCommand PrintCommand { get; }

        public IAsyncCommand PrintTtnCommand { get; }

        public IAsyncCommand PrintInvoiceTtnCommand { get; }

        public IAsyncCommand PrintSendingCommand { get; }

        public IDelegateCommand AddProductCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IAsyncCommand CancelMovementCommand { get; }

        public IDelegateCommand ShowQuantityInSerialsCommand { get; }

        public IDelegateCommand ShowQuantityOutSerialsCommand { get; }

        #endregion Commands

        #region INPC

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel
        {
            get { return GetProperty(() => RecognizeBarcodeViewModel); }
            private init { SetProperty(() => RecognizeBarcodeViewModel, value); }
        }

        public MovementProductViewItem SelectedMovementProduct
        {
            get { return GetProperty(() => SelectedMovementProduct); }
            set { SetProperty(() => SelectedMovementProduct, value, LoadValues); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private init { SetProperty(() => ProductInformation, value); }
        }
        #endregion INPC

        #region DialogSettings

        public override int Width => 920;

        public override int Height => 506;

        public override int MinWidth => 920;

        public override int MinHeight => 420;

        #endregion DialogSettings

        public bool IsLocked => Model?.EmployeeLockId != null && Model.EmployeeLockId > 0;

        public bool HasNewState => Model?.State == MovementState.New;

        public bool HasLeftState => Model?.State == MovementState.Left;

        public bool HasArrivedState => Model?.State == MovementState.Arrived;

        public bool HasReceivedState => Model?.State == MovementState.Received;

        public bool HasLeftOrReceivedState => Model?.State == MovementState.Left || Model?.State == MovementState.Received;

        public bool IsNewAndWarehouseFromAllowed => HasNewState && IsWarehouseAllowed(Model.WarehouseFromId);

        public bool IsNewAndWarehouseToAllowed => HasNewState && IsWarehouseAllowed(Model.WarehouseToId);

        public bool IsLeftAndWarehouseToAllowed => HasLeftState && IsWarehouseAllowed(Model.WarehouseToId);

        public bool IsArrivedAndWarehouseToAllowed => HasArrivedState && IsWarehouseAllowed(Model.WarehouseToId);

        public bool IsEditVisible => (AllowAddOrDeleteProduct && Model?.State == MovementState.New) || IsNewAndWarehouseFromAllowed || IsArrivedAndWarehouseToAllowed;

        public bool AllowAddOrDeleteProduct { get; }

        protected override string CreatedActionMessage { get; } = "создано";

        protected override string EntityName { get; } = "Перемещение";

        protected override string UpdatedActionMessage { get; } = "сохранено";

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IMediator Mediator { get; }

        private IPriceConverterFactory PriceConverterFactory { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IErrorHandler ErrorHandler { get; }

        public override void OnDestroy()
        {
            if (Model != null)
            {
                foreach (MovementProductViewItem movementProductViewItem in Model.MovementProducts)
                {
                    movementProductViewItem.PropertyChanged -= MovementViewItemPropertyChanged;
                }
            }

            base.OnDestroy();
        }

        protected override void BeforeSetData(MovementViewItem model, object dto)
        {
            MovementDto movementDto = (MovementDto)dto;

            try
            {
                if (Model != null)
                {
                    foreach (MovementProductViewItem movementProductViewItem in Model.MovementProducts)
                    {
                        movementProductViewItem.PropertyChanged -= MovementViewItemPropertyChanged;
                    }
                }

                SplitAssemblyServices(movementDto, model);
                SplitAssembledComputers(movementDto, model);

                model.MovementProducts = model.MovementProducts
                    .OrderByDescending(x => x.TypeId == ProductType.AssemblyServiceId)
                    .ThenBy(x => x.ProductParentCategoryLeft)
                    .ThenBy(x => x.ProductName)
                    .ToObservableCollection();

                SplitAdditionalServices(movementDto, model);

                model.MovementProducts.Where(x => x.GroupString is null).ForEach(x => x.GroupString = "Товары");

                model.MovementProducts = model.MovementProducts.ToObservableCollection();

                int[] orderIds = movementDto.AssembledComputers.Select(x => x.OrderId).ToArray();

                _anotherOrdersNomenclatureSeries = _nomenclatureSeriesWithOrders
                    .Where(x => !orderIds.Contains(x.Value))
                    .Select(x => x.Key)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load movement");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        protected override Task<Result<MovementDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(MovementDto dto, MessageType messageType)
        {
            return new MovementMessage(dto, messageType);
        }

        protected override Task<MovementDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryMovement(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            MovementViewMessage p = (MovementViewMessage)Parameter;

            (Result _, List<CategoryDto> categories, PagedResult<EmployeeDto> employees, Result __) result = await TaskExt.WhenAll(
                FetchProductAttributesAsync(p.Id),
                WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync(),
                WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true),
                FetchAssemblyServicesAsync());

            categories = result.categories.ToDictionary(x => x.Id);

            employees = result.employees.Data.ToDictionary(x => x.Id, x => x.Name);

            await base.HandleLoadedAsync();

            RecognizeBarcodeViewModel.Init(
                new RecognizeBarcodeSettings(
                    true,
                    true,
                    allowOurAssemblyService: true,
                    additionalServiceProductIds: additionalServiceProducts.Select(x => x.AdditionalServiceProductId).ToArray()),
                productAttributes);

            _warehouseFrom = await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(Model.WarehouseFromId));
        }

        protected override Task<LockResponse<MovementDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockMovement(id));
        }

        protected override Task<LockResponse<MovementDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockMovement(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"Перемещение №{Model.Id}";
        }

        protected override async Task<Result<MovementDto>> UpdateEntityAsync()
        {
            MovementProductSaveDto[] productsToSave = Model.MovementProducts
                .Where(x => x.AdditionalServiceProductId is null)
                .GroupBy(x => x.ProductId)
                .Select(
                    x => new MovementProductSaveDto(
                        x.Key,
                        x.Sum(y => y.Quantity),
                        x.Sum(y => y.QuantityOut),
                        x.Sum(y => y.QuantityIn),
                        x.Where(z => z.SerialNumbers != null)
                            .SelectMany(z => z.SerialNumbers)
                            .Select(
                                z => new MovementProductSnDto()
                                {
                                    Id = z.Id,
                                    ScannedIn = z.ScannedIn,
                                    ScannedOut = z.ScannedOut,
                                    Sn = z.Sn,
                                    NomenclatureSeries = z.NomenclatureSeries
                                }).ToList()))
                .ToArray();

            MovementAdditionalServiceProductDto[] additionalServiceProductsToSave = Model.MovementProducts
                .Where(x => x.AdditionalServiceProductId != null)
                .Select(x => new MovementAdditionalServiceProductDto()
                {
                    ScannedIn = x.QuantityIn > 0,
                    ScannedOut = x.QuantityOut > 0,
                    AdditionalServiceProductId = x.AdditionalServiceProductId.Value,
                    ProductId = x.ProductId
                })
                .ToArray();

            MovementAssemblyServiceProductDto[] assemblyServiceProductsToSave = Model.MovementProducts
                .Where(x => x.AssemblyServiceId.HasValue && x.AssemblyServiceProductId.HasValue)
                .Select(x => new MovementAssemblyServiceProductDto()
                {
                    Quantity = x.Quantity,
                    ProductId = x.ProductId,
                    ScannedIn = x.QuantityIn > 0,
                    ScannedOut = x.QuantityOut > 0,
                    AssemblyServiceProductId = x.AssemblyServiceProductId.Value,
                    AssemblyServiceId = x.AssemblyServiceId.Value
                })
                .ToArray();

            MovementUpdateDto dto = new MovementUpdateDto
            {
                Products = productsToSave,
                AdditionalServiceProducts = additionalServiceProductsToSave,
                AssemblyServiceProducts = assemblyServiceProductsToSave
            };

            Result<MovementDto> updateResult = await WebClient.ExecuteApiRequestAsync(new UpdateMovement(Model.Id, dto));

            await FetchProductAttributesAsync(Model.Id);

            return updateResult;
        }

        protected override void AfterSetData()
        {
            foreach (MovementProductViewItem movementProductViewItem in Model.MovementProducts)
            {
                movementProductViewItem.PropertyChanged += MovementViewItemPropertyChanged;
            }

            Model.MovementProducts.ForEach(x => x.ProductParentCategoryId = productParentCategories[x.ProductId]);
            ModelOriginal.MovementProducts.ForEach(x => x.ProductParentCategoryId = productParentCategories[x.ProductId]);

            Model.MovementProducts = ProcessProducts(Model.MovementProducts).ToObservableCollection();
            ModelOriginal.MovementProducts = ProcessProducts(ModelOriginal.MovementProducts).ToObservableCollection();

            RaisePropertiesChanged(
                nameof(HasNewState),
                nameof(HasLeftState),
                nameof(HasArrivedState),
                nameof(HasReceivedState),
                nameof(HasLeftOrReceivedState),
                nameof(IsNewAndWarehouseFromAllowed),
                nameof(IsNewAndWarehouseToAllowed),
                nameof(IsLeftAndWarehouseToAllowed),
                nameof(IsArrivedAndWarehouseToAllowed),
                nameof(IsEditVisible),
                nameof(IsChanged),
                nameof(IsLocked),
                nameof(IsLockedByCurrentEmployee));

            RefreshSummaryItems();
        }

        private IEnumerable<MovementProductViewItem> ProcessProducts(IReadOnlyCollection<MovementProductViewItem> movementProducts)
        {
            foreach (MovementProductViewItem movementProductViewItem in movementProducts)
            {
                CategoryDto parentCategory = categories.GetValueOrDefault(movementProductViewItem.ProductParentCategoryId);

                if (parentCategory != null)
                {
                    movementProductViewItem.ProductParentCategoryName = parentCategory.GetLocalName(LocalizableNameType.Ukr);
                    movementProductViewItem.ProductParentCategoryLeft = parentCategory.Left;
                }
            }

            return movementProducts;
        }

        private bool ConfirmMovement(MovementViewItem movement)
        {
            MovementState state = movement.State;

            Func<MovementProductViewItem, int> getExtectedQuantity;
            Func<MovementProductViewItem, int> getActualQuantity;
            string confirmMessage;

            if (state == MovementState.New)
            {
                getExtectedQuantity = x => x.Quantity;
                getActualQuantity = x => x.QuantityOut;
                confirmMessage = "Вы уверены, что хотите отправить перемещение?";
            }
            else if (state == MovementState.Arrived)
            {
                getExtectedQuantity = x => x.QuantityOut;
                getActualQuantity = x => x.QuantityIn;
                confirmMessage = "Вы уверены, что хотите принять перемещение?";
            }
            else
            {
                throw new InvalidOperationException();
            }

            bool confirmed;

            List<ProductComparisonResult> items = movement.MovementProducts
                .Select(x => new ProductComparisonResult(x.ProductFullName, getExtectedQuantity(x), getActualQuantity(x)))
                .Where(x => x.DeviationQuantity != 0)
                .ToList();

            if (items.Any())
            {
                bool allowContine = state != MovementState.New || items.All(x => x.DeviationQuantity <= 0);

                ProductComparisonResultViewModel viewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(
                    new object[] { items, allowContine },
                    this);

                confirmed = viewModel.IsOk;
            }
            else
            {
                confirmed = MessageFacadeService.Confirm(confirmMessage);
            }

            return confirmed;
        }

        private void SerializeMovementAndSaveToLogs()
        {
            string rawMovement = System.Text.Json.JsonSerializer.Serialize(Model);

            Logger.LogInformation(message: rawMovement);

            MessageFacadeService.ShowMessageBoxWarning("Данные для поиска ошибки сохранены в логи.\nОтправьте логи в IT отдел.");
        }

        private void ExportToExcel(TableView tableView)
        {
            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();
                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));
                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"Movement_{Model.Id}_{DateTime.Now:yyyy-MM-dd}");
        }

        private void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            if (!IsNewAndWarehouseFromAllowed && !IsArrivedAndWarehouseToAllowed)
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на склад");
                return;
            }

            if (Model.MovementProducts.Any(x => x.ManualAdded))
            {
                e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Error, "Некоторые товары добавлены вручную. Для сканирования необходимо сохранить перемещение");
                return;
            }

            string? error = null;

            switch (e.Result)
            {
                case RecognizeBarcodeResult.FoundInSupplier:
                case RecognizeBarcodeResult.Found:
                {
                    MovementProductViewItem[] products = Model.MovementProducts
                        .Where(x => x.ProductId == e.ProductId)
                        .ToArray();

                    MovementProductViewItem firstProduct = products
                        .OrderBy(x => x.QuantityIn >= x.Quantity && x.QuantityOut >= x.Quantity)
                        .ThenBy(x => x.AssemblyServiceId.HasValue)
                        .ThenBy(x => x.AdditionalServiceProductId.HasValue)
                        .ThenBy(x => x.IsConsumableAdditionalServiceProduct)
                        .ThenBy(x => x.ParentRowRef.HasValue)
                        .FirstOrDefault();

                    if (firstProduct is null)
                    {
                        error = "Такого товара нет в перемещении";
                        break;
                    }

                    if (firstProduct.AssemblyServiceId.HasValue)
                    {
                        error = "Сканирование товара внутри сборки необходимо производить путем сканирования всей сборки";
                        break;
                    }

                    if (firstProduct.AdditionalServiceProductId.HasValue)
                    {
                        error = "Сканирование товаров-услуг необходимо производить путем сканирования кодов услуг";
                        break;
                    }

                    if (Model.MovementProducts.Any(z => z.ParentRowRef == firstProduct.RowRef)
                        || Model.MovementProducts.Any(z => z.RowRef == firstProduct.ParentRowRef && z.AdditionalServiceProductId.HasValue))
                    {
                        error = "Необходимо начать сканирование с услуг";
                        break;
                    }

                    if (firstProduct.TypeId == ProductType.AssembledComputerRuleId)
                    {
                        ProcessScanForAssembledComputers(firstProduct, products);
                    }
                    else
                    {
                        firstProduct.Scan(Model.State.Id, e.Quantity);
                    }

                    SelectedMovementProduct = firstProduct;

                    RaisePropertyChanged(nameof(IsChanged));
                    break;
                }

                case RecognizeBarcodeResult.FoundAdditionalServiceProduct:
                {
                    MovementProductViewItem additionalServiceMovementItem = Model.MovementProducts
                        .OrderByDescending(x => x.Quantity - x.QuantityIn)
                        .ThenByDescending(x => x.Quantity - x.QuantityOut)
                        .First(x => x.AdditionalServiceProductId == e.AdditionalServiceProductId);

                    if ((additionalServiceMovementItem.QuantityOut > 0 && (HasNewState || HasLeftState))
                        || (additionalServiceMovementItem.QuantityIn > 0 && (HasReceivedState || HasArrivedState)))
                    {
                        error = "Услуга уже просканирована";
                        break;
                    }

                    additionalServiceMovementItem.Scan(Model.State.Id, e.Quantity);

                    MovementProductViewItem[] childConsumableProducts = Model.MovementProducts
                        .Where(x => x.ParentRowRef == additionalServiceMovementItem.RowRef)
                        .ToArray();

                    foreach (MovementProductViewItem childConsumableProduct in childConsumableProducts)
                    {
                        childConsumableProduct.Scan(Model.State.Id, e.Quantity);
                    }

                    MovementProductViewItem parentProduct = Model.MovementProducts.FirstOrDefault(x => x.RowRef == additionalServiceMovementItem.ParentRowRef);

                    if (parentProduct is not null)
                    {
                        MovementProductViewItem[] parentProductAdditionalServices = Model.MovementProducts
                            .Where(x => x.ParentRowRef == parentProduct.RowRef && x.AdditionalServiceProductId != null)
                            .ToArray();

                        if (parentProductAdditionalServices.All(x => x.IsScanned(Model.State.Id)))
                        {
                            parentProduct.Scan(Model.State.Id, e.Quantity);

                            if (parentProduct.TypeId == ProductType.AssembledComputerRuleId)
                            {
                                string sn = string.IsNullOrEmpty(
                                    additionalServiceMovementItem.AdditionalServiceProductSerialNumber)
                                    ? additionalServiceMovementItem.PrimaryAdditionalServiceProductSerialNumber
                                    : additionalServiceMovementItem.AdditionalServiceProductSerialNumber;

                                parentProduct.SerialNumbers.Add(new MovementProductSnViewItem
                                {
                                    Id = 0,
                                    NomenclatureSeries = sn,
                                    Sn = sn,
                                    NotSaved = true,
                                    ScannedOut = Model.State.Id == MovementState.New.Id,
                                    ScannedIn = Model.State.Id == MovementState.Arrived.Id
                                });
                            }
                        }

                        if (parentProduct.AssemblyServiceId.HasValue)
                        {
                            MovementProductViewItem[] assemblyItems = Model.MovementProducts
                                .Where(x => x.AssemblyServiceId == parentProduct.AssemblyServiceId)
                                .ToArray();

                            Guid[] assemblyItemsRowRefs = assemblyItems.Select(x => x.RowRef).ToArray();

                            MovementProductViewItem[] assemblyAdditionalServiceItems = Model.MovementProducts
                                .Where(x => x.ParentRowRef.HasValue && assemblyItemsRowRefs.Contains(x.ParentRowRef.Value) && x.AdditionalServiceProductId.HasValue)
                                .ToArray();

                            if (assemblyAdditionalServiceItems.All(x => x.IsScanned(Model.State.Id)))
                            {
                                foreach (MovementProductViewItem item in assemblyItems)
                                {
                                    item.ScanAssemblyProduct(Model.State.Id);
                                }
                            }
                        }
                    }

                    RaisePropertyChanged(nameof(IsChanged));
                    break;
                }

                case RecognizeBarcodeResult.FoundAssembly:
                {
                    MovementProductViewItem[] assemblyItems = Model.MovementProducts
                        .Where(x => x.AssemblyServiceId == e.AssemblyServiceId)
                        .ToArray();

                    if (assemblyItems.Length == 0)
                    {
                        error = "Такой сборки нет в перемещении";
                        break;
                    }

                    if (assemblyItems.Any(x => x.IsScanned(Model.State.Id)))
                    {
                        error = "Сборка уже просканирована";
                        break;
                    }

                    if (assemblyItems.Any(x => Model.MovementProducts.Any(z => z.ParentRowRef == x.RowRef && z.AdditionalServiceProductId.HasValue)))
                    {
                        error = "Сканировать сборку с услугами необходимо через сканирование услуг";
                        break;
                    }

                    foreach (MovementProductViewItem item in assemblyItems)
                    {
                        item.Scan(Model.State.Id, item.Quantity);
                    }

                    RaisePropertyChanged(nameof(IsChanged));
                    break;
                }

                case RecognizeBarcodeResult.NotFound:
                case RecognizeBarcodeResult.Error:
                {
                    error = e.Message.MessageText;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                e.WarningAction = () => MessageFacadeService.ShowCustomMessageBox(error, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning, true);

                e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, error);
                return;
            }
        }

        private async Task FastSendMovementAsync()
        {
            if (Model.CarryId.HasValue)
            {
                MessageFacadeService.ShowNotificationError("Не корректный способ доставки");
                return;
            }

            if (!ConfirmMovement(Model))
            {
                return;
            }

            await ExecuteLockableOperationAsync(
                async _ =>
                {
                    Result<MovementDto> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new FastSendMovement(Model.Id)), "быстром отправлении", "Перемещение отправлено", this, true);

                    if (result?.Data is null)
                    {
                        return;
                    }

                    Messenger.Send(new MovementMessage(result.Data, MessageType.Changed));
                });
        }

        private void SplitAdditionalServices(MovementDto dto, MovementViewItem model)
        {
            if (dto.AdditionalServiceProducts?.Any() != true)
            {
                return;
            }

            additionalServiceProducts = dto.AdditionalServiceProducts.ToArray();

            foreach (MovementAdditionalServiceProductDto additionalServiceProductDto in additionalServiceProducts)
            {
                MovementProductViewItem movementProductViewItem = model.MovementProducts
                        .OrderBy(x => x.AdditionalServicesQuantity)
                        .ThenByDescending(x => x.Quantity)
                        .FirstOrDefault(x => x.ProductId == (additionalServiceProductDto.ParentOrderProductProductId ?? additionalServiceProductDto.ProductId) && (x.OrderIds?.Any() != true || x.OrderIds.Contains(additionalServiceProductDto.OrderId)));

                if (movementProductViewItem == null)
                {
                    continue;
                }

                MovementProductViewItem splittedMovementProduct;

                if (movementProductViewItem.Quantity > 1)
                {
                    if (additionalServiceProductDto.ScannedIn && movementProductViewItem.QuantityIn == 0)
                    {
                        var movementProductToGrabQuantity = model.MovementProducts
                            .FirstOrDefault(x => x.QuantityIn > 0 && x.ProductId == movementProductViewItem.ProductId);
                        if (movementProductToGrabQuantity != null)
                        {
                            movementProductViewItem.QuantityIn++;
                            movementProductToGrabQuantity.QuantityIn--;
                        }
                    }

                    if (additionalServiceProductDto.ScannedOut && movementProductViewItem.QuantityOut == 0)
                    {
                        var movementProductToGrabQuantity = model.MovementProducts
                            .FirstOrDefault(x => x.QuantityOut > 0 && x.ProductId == movementProductViewItem.ProductId);
                        if (movementProductToGrabQuantity != null)
                        {
                            movementProductViewItem.QuantityOut++;
                            movementProductToGrabQuantity.QuantityOut--;
                        }
                    }

                    splittedMovementProduct = movementProductViewItem.SplitAdditionalService(
                        additionalServiceProductDto.ScannedIn,
                        additionalServiceProductDto.ScannedOut);

                    model.MovementProducts.Add(splittedMovementProduct);
                }
                else
                {
                    splittedMovementProduct = movementProductViewItem;
                }

                splittedMovementProduct.AdditionalServicesQuantity++;

                MovementProductViewItem additionalServiceMovementItem = new MovementProductViewItem()
                {
                    ProductId = additionalServiceProductDto.ProductIdFromAdditionalService,
                    ProductName = additionalServiceProductDto.AdditionalServiceName,
                    ProductNameUa = additionalServiceProductDto.AdditionalServiceNameUa,
                    ProductNameEn = additionalServiceProductDto.AdditionalServiceNameEn,
                    AssemblyServiceId = splittedMovementProduct.AssemblyServiceId,
                    AdditionalServiceProductId = additionalServiceProductDto.AdditionalServiceProductId,
                    AdditionalServiceProductToolTip = $"Оказанная услуга по заказу №{additionalServiceProductDto.OrderId}",
                    ParentRowRef = splittedMovementProduct.RowRef,
                    Quantity = 1,
                    QuantityIn = additionalServiceProductDto.ScannedIn ? 1 : 0,
                    QuantityOut = additionalServiceProductDto.ScannedOut ? 1 : 0,
                    GroupString = splittedMovementProduct.GroupString,
                    AdditionalServiceProductSerialNumber = additionalServiceProductDto.SerialNumber,
                    PrimaryAdditionalServiceProductSerialNumber = additionalServiceProductDto.PrimaryAdditionalServiceProductSn
                };

                int additionalServiceIndex = model.MovementProducts.IndexOf(splittedMovementProduct) + 1;

                if (additionalServiceIndex > model.MovementProducts.Count)
                {
                    model.MovementProducts.Add(additionalServiceMovementItem);
                }
                else
                {
                    model.MovementProducts.Insert(additionalServiceIndex, additionalServiceMovementItem);
                }

                if (additionalServiceProductDto.ConsumableProducts?.Any() == true)
                {
                    foreach (AdditionalServiceProductConsumableDto additionalServiceProductConsumableDto in
                             additionalServiceProductDto.ConsumableProducts)
                    {
                        MovementProductViewItem additionalServiceConsumableViewItem = model.MovementProducts
                            .Where(x => !x.IsConsumableAdditionalServiceProduct)
                            .OrderByDescending(x => x.Quantity)
                            .FirstOrDefault(x => x.ProductId == additionalServiceProductConsumableDto.ProductId);

                        if (additionalServiceConsumableViewItem is null)
                        {
                            continue;
                        }

                        MovementProductViewItem splittedConsumableMovementProduct =
                            additionalServiceConsumableViewItem.SplitConsumableAdditionalServiceProduct(
                                additionalServiceMovementItem.QuantityIn > 0,
                                additionalServiceMovementItem.QuantityOut > 0,
                                additionalServiceMovementItem.GroupString,
                                additionalServiceMovementItem.RowRef);

                        splittedConsumableMovementProduct.AssemblyServiceId = additionalServiceMovementItem.AssemblyServiceId;

                        if (ReferenceEquals(splittedConsumableMovementProduct, additionalServiceConsumableViewItem))
                        {
                            model.MovementProducts.Remove(splittedConsumableMovementProduct);
                        }

                        int additionalServiceConsumableIndex = model.MovementProducts.IndexOf(splittedMovementProduct) + 2;

                        if (additionalServiceConsumableIndex > model.MovementProducts.Count)
                        {
                            model.MovementProducts.Add(splittedConsumableMovementProduct);
                        }
                        else
                        {
                            model.MovementProducts.Insert(additionalServiceConsumableIndex, splittedConsumableMovementProduct);
                        }
                    }
                }
            }
        }

        private void SplitAssemblyServices(MovementDto dto, MovementViewItem model)
        {
            if (dto.AssemblyServiceProducts?.Any() != true)
            {
                return;
            }

            foreach (MovementAssemblyServiceProductDto assemblyServiceProduct in dto.AssemblyServiceProducts)
            {
                MovementProductViewItem movementProductViewItem = model.MovementProducts
                    .FirstOrDefault(x => x.ProductId == assemblyServiceProduct.ProductId && x.AssemblyServiceId == null);

                if (movementProductViewItem == null)
                {
                    continue;
                }

                MovementProductViewItem splittedMovementProduct = movementProductViewItem.SplitAssemblyService(
                    assemblyServiceProduct.Quantity,
                    assemblyServiceProduct.ScannedIn,
                    assemblyServiceProduct.ScannedOut);

                splittedMovementProduct.GroupString = $"🖥️ Сборка №{assemblyServiceProduct.AssemblyServiceId}, Заказ №{assemblyServiceProduct.OrderId}";
                splittedMovementProduct.OrderIds = new[] { assemblyServiceProduct.OrderId };
                splittedMovementProduct.AssemblyServiceId = assemblyServiceProduct.AssemblyServiceId;
                splittedMovementProduct.AssemblyServiceProductId = assemblyServiceProduct.AssemblyServiceProductId;

                if (!ReferenceEquals(splittedMovementProduct, movementProductViewItem))
                {
                    model.MovementProducts.Add(splittedMovementProduct);
                }
            }
        }

        private void SplitAssembledComputers(MovementDto dto, MovementViewItem model)
        {
            if (dto.AssembledComputers?.Any() != true)
            {
                return;
            }

            movementAssembledComputersNomenclatureSeries = dto.AssembledComputers
                .Select(x => (x.ProductId, x.NomenclatureSeries))
                .ToArray();

            StringBuilder orderAssembledComputersToolTip = new StringBuilder();

            MovementProductViewItem[] assembledComputerProducts = model.MovementProducts.Where(x => x.TypeId == ProductType.AssembledComputerRuleId).ToArray();

            foreach (MovementProductViewItem assembledComputerMovementProduct in assembledComputerProducts)
            {
                MovementAssembledComputerDto[] currentViewItemAssembledComputers = dto.AssembledComputers
                    .Where(x => x.ProductId == assembledComputerMovementProduct.ProductId)
                    .ToArray();

                if (currentViewItemAssembledComputers.Any())
                {
                    MovementProductViewItem splittedMovementProduct = assembledComputerMovementProduct.SplitAssembledComputer(
                        currentViewItemAssembledComputers.Select(x => x.NomenclatureSeries).ToArray(),
                        model.State.Id);

                    splittedMovementProduct.GroupString = "🖥️ Конфигурации ПК под заказы";
                    splittedMovementProduct.AssembledComputerForOrder = true;
                    splittedMovementProduct.OrderIds = currentViewItemAssembledComputers.Select(x => x.OrderId).ToArray();

                    orderAssembledComputersToolTip.AppendLine(string.Join("\n", currentViewItemAssembledComputers.Select(x => $"заказ: {x.OrderId} SN: {x.NomenclatureSeries}")));

                    if (!ReferenceEquals(splittedMovementProduct, assembledComputerMovementProduct))
                    {
                        model.MovementProducts.Add(splittedMovementProduct);

                        assembledComputerMovementProduct.GroupString = OtherConfigurationStr;
                    }
                }
                else
                {
                    assembledComputerMovementProduct.GroupString = OtherConfigurationStr;
                }
            }

            string assembledComputerForOrderToolTip = orderAssembledComputersToolTip.ToString();

            model.MovementProducts
                .Where(x => x.AssembledComputerForOrder)
                .ForEach(x => x.AssembledComputerForOrderToolTip = assembledComputerForOrderToolTip);
        }

        private async Task SendMovementAsync()
        {
            if (!ConfirmMovement(Model))
            {
                return;
            }

            int places = 0;

            if (Model.CarryId is CarryType.NpDeliveryId or CarryType.NpWarehouseId)
            {
                if (string.IsNullOrWhiteSpace(Model.TrackNumber)
                    || (!string.IsNullOrWhiteSpace(Model.TrackNumber) && MessageFacadeService.Confirm($"Для перемещения {Model.Id} заполнена ТТН {Model.TrackNumber}. Создать новую ТТН вместо созданной?")))
                {
                    PackagePropertiesViewModel propertiesViewModel = await GetPackagePropertiesAsync();

                    if (!propertiesViewModel.IsOk)
                    {
                        return;
                    }

                    places = (int)propertiesViewModel.PackagePlaces;

                    NpDocumentDto npDocument = await CreateTtnAsync(places, (double)propertiesViewModel.TotalWeight, propertiesViewModel.Insurance, !propertiesViewModel.NotAddToNpApplication);

                    if (npDocument is null)
                    {
                        return;
                    }
                }
                else
                {
                    try
                    {
                        NpDocumentDto npDocument = await WebClient.ExecuteApiRequestAsync(new QueryNpDocument(Model.TrackNumber));

                        MovementPlacesViewModel placesViewModel = ShowPlacesView();

                        if (!placesViewModel.IsOk)
                        {
                            return;
                        }

                        places = placesViewModel.Places;

                        await Mediator.Send(new PrintTrackNumberRequest(npDocument.Id, npDocument.Link, true));
                    }
                    catch (Exception)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН");
                        return;
                    }
                }
            }
            else if (Model.CarryId == CarryType.TeksId)
            {
                (bool IsageReasonCreateDto, int Places) createTtnResult = await CreateTeksTtnAsync();

                if (!createTtnResult.IsageReasonCreateDto)
                {
                    return;
                }

                places = createTtnResult.Places;
            }
            else
            {
                MovementPlacesViewModel placesViewModel = ShowPlacesView();

                if (!placesViewModel.IsOk)
                {
                    return;
                }

                places = placesViewModel.Places;
            }

            await ExecuteLockableOperationAsync(
                async lockedEntity =>
                {
                    Result<MovementDto> result = await WebClient.ExecuteApiRequestAsync(new SendMovement(lockedEntity.Id, places));

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} отправлено с предупреждениями");

                        ShowValidationResultView(
                            "Предупрежедения при отправке перемещения",
                            result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} успешно отправлено");
                    }

                    Messenger.Send(new MovementMessage(result.Data, MessageType.Changed));

                    await PrintMovementPlaceAsync(places);
                });
        }

        private async Task<(bool, int)> CreateTeksTtnAsync()
        {
            int places = Model.Places ?? 0;

            if (!string.IsNullOrWhiteSpace(Model.TrackNumber))
            {
                return (true, places);
            }

            PackagePropertiesViewModel propertiesViewModel = await GetPackagePropertiesAsync();

            if (propertiesViewModel.IsOk)
            {
                CreateTeksDocumentDto dto = new CreateTeksDocumentDto()
                {
                    MovementId = Model.Id,
                    TeksPackages = propertiesViewModel.PackagePlaceItems.Select(x => new TeksPackageDto
                    {
                        PackagePlaces = (int) propertiesViewModel.PackagePlaces,
                        VolumeWeight = propertiesViewModel.VolumeWeight,
                        Height = x.Height,
                        Width = x.Width,
                        Length = x.Length,
                        PackageInsurance = x.Insurance,
                        PackageWeight = x.Weight
                    }).ToArray(),
                };

                Result<MovementDto> movementResult = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateTeksTtnByMovement(dto)),
                    "создании ТТН",
                    "ТТН создана",
                    this,
                    true);

                if (!movementResult.IsSuccess)
                {
                    return (false, 0);
                }

                Model.TrackNumber = ModelOriginal.TrackNumber = movementResult.Data.TrackNumber;

                places = (int)propertiesViewModel.PackagePlaces;

                ITrackNumberProvider trackNumberProvider = Model.Carry.GetTrackNumberProvider();

                await trackNumberProvider.PrintAsync(Model.TrackNumber, false);

                if (Model.Carry.Id == CarryType.TeksId && trackNumberProvider is TeksTrackNumberProvider provider)
                {
                    provider.InvoiceTtn = true;

                    await trackNumberProvider.PrintAsync(Model.TrackNumber, false);
                }

                return (true, places);
            }

            return (false, places);
        }

        private async Task<PackagePropertiesViewModel> GetPackagePropertiesAsync()
        {
            IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

            PackagePropertiesViewModel packagePropertiesViewModel = ShowPackagePropertiesView(priceConverter);

            return packagePropertiesViewModel;
        }

        private Task ReceiveMovementAsync()
        {
            if (!ConfirmMovement(Model))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(
                async lockedEntity =>
                {
                    Result<MovementDto> result = await WebClient.ExecuteApiRequestAsync(new ReceiveMovement(lockedEntity.Id));

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} принято с предупреждениями");

                        ShowValidationResultView(
                            "Предупрежедения при принятии перемещения",
                            result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} успешно принято");
                    }

                    Messenger.Send(new MovementMessage(result.Data, MessageType.Changed));
                });
        }

        private Task ArriveMovementAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(
                async lockedEntity =>
                {
                    Result<MovementDto> result = await WebClient.ExecuteApiRequestAsync(new ArriveMovement(lockedEntity.Id));

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} приехало с предупреждениями");

                        ShowValidationResultView(
                            "Предупрежедения",
                            result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} успешно приехало");
                    }

                    Messenger.Send(new MovementMessage(result.Data, MessageType.Changed));
                });
        }

        private void MovementViewItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(IsChanged));
        }

        private bool IsWarehouseAllowed(int warehouseId)
        {
            return WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(warehouseId);
        }

        private bool CanDelete(MovementProductViewItem item)
        {
            return item != null
                   && Model != null
                   && IsLockedByCurrentEmployee
                   && ((item.CreatedBy != null && Model.State == MovementState.New)
                       || (item.TypeId == ProductType.AssembledComputerRuleId && !item.AssembledComputerForOrder && (Model.State == MovementState.New || Model.State == MovementState.Arrived)));
        }

        private async void RefreshSummaryItems()
        {
            try
            {
                IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

                SummaryItems = GetSummaryItems();

                IEnumerable<SummaryViewItem> GetSummaryItems()
                {
                    employees.TryGetValue(Model.CreatedBy, out string createdBy);
                    employees.TryGetValue(Model.SentBy ?? 0, out string sentBy);
                    employees.TryGetValue(Model.ReceivedBy ?? 0, out string receivedBy);

                    yield return new SummaryViewItem("Создал", $"{createdBy} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

                    yield return new SummaryViewItem("Отправка", $"{Model.DateDeparture.ToString(DateFormattingRules.FullDateTimeFormat)}");

                    if (Model.SentBy.HasValue && Model.SentOn.HasValue)
                    {
                        yield return new SummaryViewItem("Отправил", $"{sentBy} ({Model.SentOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)})");
                    }

                    yield return new SummaryViewItem("Прибытие", $"{Model.DateArrive.ToString(DateFormattingRules.FullDateTimeFormat)}");

                    if (Model.ArrivedOn.HasValue)
                    {
                        yield return new SummaryViewItem("Прибыло", $"{Model.ArrivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)}");
                    }

                    yield return new SummaryViewItem("Получение", $"{Model.DateIn.ToString(DateFormattingRules.FullDateTimeFormat)}");

                    if (Model.ReceivedBy.HasValue && Model.ReceivedOn.HasValue)
                    {
                        yield return new SummaryViewItem("Принял", $"{receivedBy} ({Model.ReceivedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)})");
                    }

                    if (Model.Carry != null)
                    {
                        yield return new SummaryViewItem("Доставка", Model.Carry.Name);
                    }

                    if (!string.IsNullOrEmpty(Model.TrackNumber))
                    {
                        yield return new SummaryViewItem("ТТН", Model.TrackNumber);
                    }

                    if (Model.Places.HasValue)
                    {
                        yield return new SummaryViewItem("Мест", Model.Places.ToString());
                    }

                    yield return new SummaryViewItem("Статус", $"{Model.State.Name}");
                    yield return new SummaryViewItem("Вес (план)", $"{GetTotalWeightPlan():N1}");
                    yield return new SummaryViewItem("Вес (факт)", $"{GetTotalWeightFact():N1}");

                    yield return new SummaryViewItem("План", $"{Math.Round(Model.MovementProducts.Where(x => x.Price > 0).Sum(x => priceConverter.Convert(x.Price ?? 0, Currency.UsdId, Currency.UahId, x.UsdCurrency, false) * x.Quantity))}");
                    yield return new SummaryViewItem("Факт", $"{Math.Round(Model.MovementProducts.Where(x => x.Price > 0).Sum(x => priceConverter.Convert(x.Price ?? 0, Currency.UsdId, Currency.UahId, x.UsdCurrency, false) * x.QuantityOut))}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to refresh summary items for movement");
            }
        }

        private void ResetQuantityIn(MovementProductViewItem viewItem)
        {
            if (MessageFacadeService.Confirm(ResetQuantityConfirmMsg))
            {
                MovementProductViewItem[] itemsToClear = GetScanGroupItems(viewItem).ToArray();

                foreach (MovementProductViewItem itemToClear in itemsToClear)
                {
                    itemToClear.QuantityIn = 0;

                    if (itemToClear.SerialNumbers?.Any() == true)
                    {
                        itemToClear.SerialNumbers.ForEach(x => x.ScannedIn = false);

                        MovementProductSnViewItem[] serialNumbersToRemove = itemToClear.SerialNumbers
                            .Where(x => x.NotSaved)
                            .ToArray();

                        serialNumbersToRemove.ForEach(x => itemToClear.SerialNumbers.Remove(x));
                    }
                }
            }
        }

        private void ResetQuantityOut(MovementProductViewItem viewItem)
        {
            if (MessageFacadeService.Confirm(ResetQuantityConfirmMsg))
            {
                MovementProductViewItem[] itemsToClear = GetScanGroupItems(viewItem).ToArray();

                foreach (MovementProductViewItem itemToClear in itemsToClear)
                {
                    itemToClear.QuantityOut = 0;
                    itemToClear.SerialNumbers = new List<MovementProductSnViewItem>();
                }
            }
        }

        private void ShowQuantityInSerials(MovementProductViewItem item)
        {
            MovementProductSnViewItem[] quantityInSerialNumbers = item.SerialNumbers.Where(x => x.ScannedIn).ToArray();

            DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(quantityInSerialNumbers.Select(x => x.Sn).ToList(), true), this);
        }

        private void ShowQuantityOutSerials(MovementProductViewItem item)
        {
            MovementProductSnViewItem[] quantityOutSerialNumbers = item.SerialNumbers.Where(x => x.ScannedOut).ToArray();

            DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(quantityOutSerialNumbers.Select(x => x.Sn).ToList(), true), this);
        }

        private void HandleRowDoubleClick()
        {
            if (SelectedMovementProduct is null
                || SelectedMovementProduct.TypeId != ProductType.AssembledComputerRuleId
                || SelectedMovementProduct.SerialNumbers?.Any() != true)
            {
                return;
            }

            ProductEditSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductEditSerialsViewModel>(
                new ProductEditSerialsParameter(SelectedMovementProduct.SerialNumbers.Select(x => x.NomenclatureSeries).ToList(), !IsLockedByCurrentEmployee), this);

            if (!viewModel.IsOk || viewModel.SerialNumbers.Count == SelectedMovementProduct.SerialNumbers.Count || !IsLockedByCurrentEmployee)
            {
                return;
            }

            SelectedMovementProduct.RefreshSerialNumbersAfterEditing(viewModel.SerialNumbers, Model.State.Id);
        }

        private async Task PrintOurBarcodeAsync(MovementProductViewItem item)
        {
            BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(item.ProductNameUa, item.ProductId, 1);

            if (result.Printer == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            PrintReportRequest printReportRequest = new PrintReportRequest(
                result.Report,
                false,
                result.Printer.Name,
                result.Printer.PaperSource);

            await Mediator.Send(printReportRequest);
        }

        private async Task CancelMovementAsync()
        {
            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>("Вы уверены?", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new CancelMovement(Model.Id));

                MessageFacadeService.ShowNotificationInfo("Перемещение успешно отменено");
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене перемещения");
                ShowValidationResultView("Ошибка при отмене перемещения", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to cancel movement");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отмене перемещения");
                Logger.LogError(exception, "Error while cancelling movement");
            }
        }

        private async Task PrintAsync()
        {
            try
            {
                PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();
                PrinterSettingsInfo printerSettings = printingSettings.Main;

                MovementReportDto reportDto = await WebClient.ExecuteApiRequestAsync(new QueryMovementReportData(Model.Id));

                IReadOnlyCollection<MovementProductGroupReportData> categoriesForReport = reportDto.Groups
                    .OrderByDescending(x => x.Products.Any(y => y.TypeId == ProductType.AssemblyServiceId))
                    .Select(x => new MovementProductGroupReportData(
                        x.Name,
                        x.Products
                            .OrderByDescending(y => y.TypeId == ProductType.AssemblyServiceId)
                            .Select(Mapper.Map<MovementProductReportData>)
                            .ToArray()))
                    .ToArray();

                MovementReportData reportData = new MovementReportData(
                    reportDto.Id,
                    reportDto.StateId,
                    reportDto.CreatedOn,
                    reportDto.WarehouseFromName,
                    reportDto.WarehouseToName,
                    DateTime.Now,
                    categoriesForReport);

                IReport report = new MovementReport { DataSource = new[] { reportData } };

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    report,
                    true,
                    printerSettings?.Name,
                    printerSettings?.PaperSource);

                await Mediator.Send(printReportRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                ShowValidationResultView("Ошибки при формировании отчета", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create report");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                Logger.LogError(exception, "Error while creating report");
            }
        }

        private void AddProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByQuantity,
                false);

            NomenclatureViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (viewModel.IsOk)
            {
                List<MovementProductViewItem> movementProducts = Model.MovementProducts.ToList();

                List<ValidationResultItem> warnings = new List<ValidationResultItem>();

                foreach (NomenclatureViewItem selectedItem in viewModel.GetSelectedItems())
                {
                    if (movementProducts.Any(x => x.ProductId == selectedItem.Id && x.AssemblyServiceId == null))
                    {
                        warnings.Add(new ValidationResultItem($"Товар {selectedItem.Name} уже присутствует в перемещении", false));
                        continue;
                    }

                    MovementProductViewItem movementProduct = new MovementProductViewItem()
                    {
                        Quantity = selectedItem.Quantity,
                        CreatedBy = WebClient.AuthenticatedEmployee.Id,
                        TypeId = selectedItem.TypeId,
                        ProductId = selectedItem.Id,
                        ProductName = selectedItem.Name,
                        ProductNameUa = selectedItem.NameUkr,
                        ProductNameEn = selectedItem.NameEn,
                        ProductPrefixRus = selectedItem.PrefixRu,
                        ProductPrefixUa = selectedItem.PrefixUa,
                        ProductParentCategoryId = selectedItem.ParentCategoryId,
                        Weight = selectedItem.Weight,
                        ManualAdded = true
                    };

                    productParentCategories[selectedItem.Id] = selectedItem.ParentCategoryId;

                    movementProduct.PropertyChanged += MovementViewItemPropertyChanged;

                    movementProducts.Add(movementProduct);
                }

                Model.MovementProducts = ProcessProducts(movementProducts).ToObservableCollection();

                if (warnings.Any())
                {
                    ShowValidationResultView("Предупреждения при добавлении товаров", warnings);
                }
            }
        }

        private void DeleteProduct(MovementProductViewItem item)
        {
            Model.MovementProducts.Remove(item);
        }

        private PackagePropertiesViewModel ShowPackagePropertiesView(IPriceConverter priceConverter)
        {
            decimal insurance = Math.Round(Model.MovementProducts.Sum(x => priceConverter.Convert(x.Price ?? 0, Currency.UsdId, Currency.UahId, x.UsdCurrency, false) * x.QuantityOut));

            return SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                new PackagePropertiesParameter(0, insurance, Model.CarryId!.Value, GetTotalWeightFact()),
                this);
        }

        private MovementPlacesViewModel ShowPlacesView()
        {
            return DialogDocumentManagerService
                .ShowView<MovementPlacesViewModel>(new MovementPlacesParameter(Model.Places, Model.MovementProducts.Sum(x => x.QuantityOut)), this);
        }

        private async Task<NpDocumentDto> CreateTtnAsync(int places, double weight, decimal insurance, bool addToApplication)
        {
            Result<NpDocumentDto> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new CreateNpTtnByMovement(Model.Id, places, weight, insurance, addToApplication)), "создании ТТН", "ТТН создана", this, true);

            if (result?.Data is null)
            {
                return null;
            }

            try
            {
                await Mediator.Send(new PrintTrackNumberRequest(result.Data.Id, result.Data.Link, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print ttn for movement");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }

            return result.Data;
        }

        private async Task PrintTttAsync()
        {
            ITrackNumberProvider trackNumberProvider = Model.Carry.GetTrackNumberProvider();

            await trackNumberProvider.PrintAsync(Model.TrackNumber, true);
        }

        private async Task PrintInvoiceTttAsync()
        {
            ITrackNumberProvider trackNumberProvider = Model.Carry.GetTrackNumberProvider();

            if (Model.Carry.Id == CarryType.TeksId && trackNumberProvider is TeksTrackNumberProvider provider)
            {
                provider.InvoiceTtn = true;
            }

            await trackNumberProvider.PrintAsync(Model.TrackNumber, true);
        }

        private async Task PrintSendingAsync()
        {
            if (Model.Places.HasValue == false)
            {
                MessageFacadeService.ShowNotificationInfo("В выбранном перемещении отсутствует количество мест");
                return;
            }

            await PrintMovementPlaceAsync(Model.Places.Value);
        }

        private void LoadValues()
        {
            ProductInformation.ClearProduct();

            if (SelectedMovementProduct != null && SelectedMovementProduct.ProductId != 0)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedMovementProduct.ProductId, Currency.UahId);
            }
        }

        private async Task PrintMovementPlaceAsync(int places)
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Dictionary<int, string> warehouseNames = warehouses.ToDictionary(x => x.Id, x => string.IsNullOrEmpty(x.NameUkr) ? x.Name : x.NameUkr);

            MovementPlaceReportData[] reportDatas = Enumerable.Range(1, places)
                .Select(x => new MovementPlaceReportData(x, places, Model.Id, warehouseNames[Model.WarehouseFromId], warehouseNames[Model.WarehouseToId]))
                .ToArray();

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();
            PrinterSettingsInfo printerSettings = printingSettings.Sticker;

            IReport report = new MovementPlaceReport { DataSource = reportDatas };

            PrintReportRequest printReportRequest = new PrintReportRequest(
                report,
                true,
                printerSettings?.Name,
                printerSettings?.PaperSource);

            await Mediator.Send(printReportRequest);
        }

        private void ProcessScanForAssembledComputers(MovementProductViewItem firstProduct, MovementProductViewItem[] items)
        {
            ProductScanSerialsViewModel viewModel = ShowScanSerialsViewForAssembledComputer(firstProduct.ProductId);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SerialNumbers.Any(x => x is null || !Regex.IsMatch(x, @"^[0-9-]*$") || x.Length < 8))
            {
                MessageFacadeService.ShowNotificationError("Неверный формат серии номенклатур");
                return;
            }

            foreach (string nomenclatureSeries in viewModel.SerialNumbers)
            {
                MovementProductViewItem assembledComputerProduct;

                if (items.Any(x => x.AssembledComputerForOrder)
                    && movementAssembledComputersNomenclatureSeries.Any(x => x.nomenclatureSeries == nomenclatureSeries && x.productId == firstProduct.ProductId))
                {
                    assembledComputerProduct = items.First(x => x.AssembledComputerForOrder);
                }
                else
                {
                    assembledComputerProduct = items.FirstOrDefault(x => !x.AssembledComputerForOrder);

                    if (_anotherOrdersNomenclatureSeries.Contains(nomenclatureSeries))
                    {
                        if (_warehouseFrom.TreatErrorsAsWarnings)
                        {
                            MessageFacadeService.ShowMessageBoxWarning("Конфигурация ПК относится к другому заказу");
                        }
                        else
                        {
                            MessageFacadeService.ShowNotificationError("Конфигурация ПК относится к другому заказу");
                            return;
                        }
                    }

                    if (assembledComputerProduct is null)
                    {
                        if (Model.State != MovementState.Arrived)
                        {
                            MessageFacadeService.ShowNotificationError("Такой конфигурации ПК нет в перемещении");
                            return;
                        }

                        assembledComputerProduct = new MovementProductViewItem()
                        {
                            AssembledComputerForOrder = false,
                            AssemblyServiceId = null,
                            AssembledComputerForOrderToolTip = null,
                            ProductId = firstProduct.ProductId,
                            CreatedBy = WebClient.AuthenticatedEmployee.Id,
                            GroupString = OtherConfigurationStr,
                            ProductName = firstProduct.ProductName,
                            ProductPrefixRus = firstProduct.ProductPrefixRus,
                            ProductPrefixUa = firstProduct.ProductPrefixUa,
                            ProductPrefixEn = firstProduct.ProductPrefixEn,
                            Quantity = 0,
                            Price = firstProduct.Price,
                            ProductParentCategoryName = firstProduct.ProductParentCategoryName,
                            ProductParentCategoryId = firstProduct.ProductParentCategoryId,
                            TypeId = firstProduct.TypeId,
                            Weight = firstProduct.Weight,
                            UsdCurrency = firstProduct.UsdCurrency,
                            ProductNameBase = firstProduct.ProductNameBase,
                            SerialNumbers = new List<MovementProductSnViewItem>()
                        };

                        Model.MovementProducts.Add(assembledComputerProduct);

                        Model.MovementProducts = Model.MovementProducts.ToObservableCollection();
                    }
                }

                assembledComputerProduct.SerialNumbers ??= new List<MovementProductSnViewItem>();

                MovementProductSnViewItem productSn = assembledComputerProduct.SerialNumbers.FirstOrDefault(x => x.NomenclatureSeries == nomenclatureSeries);

                if (productSn is null)
                {
                    productSn = new MovementProductSnViewItem()
                    {
                        Id = 0,
                        NomenclatureSeries = nomenclatureSeries,
                        Sn = nomenclatureSeries,
                        NotSaved = true
                    };

                    assembledComputerProduct.SerialNumbers.Add(productSn);
                }

                productSn.Scan(Model.State.Id);

                assembledComputerProduct.Scan(Model.State.Id, 1);

                assembledComputerProduct.RaiseProperties();
            }

            scanSerialMode = viewModel.ScanMode;
        }

        private ProductScanSerialsViewModel ShowScanSerialsViewForAssembledComputer(int productId)
        {
            string[] barcodes = RecognizeBarcodeViewModel.GetBarcodesById(productId).ToArray();

            string[] existingNomenclaturesSeries = Model.MovementProducts
                    .Where(x => x.SerialNumbers?.Any() == true)
                    .SelectMany(x => x.SerialNumbers)
                    .Where(x => (HasArrivedState && x.ScannedIn) || (HasNewState && x.ScannedOut))
                    .Select(x => x.NomenclatureSeries)
                    .ToArray();

            ProductSerialsViewModelParameter parameter = new ProductSerialsViewModelParameter(
                productId,
                existingNomenclaturesSeries,
                barcodes,
                null,
                scanSerialMode,
                accountingSystemSerials[productId],
                title: "Сканирование SN конфигурации ПК",
                ignoreLengthValidation: true);

            ProductScanSerialsViewModel viewModel = DialogDocumentManagerService.ShowView<ProductScanSerialsViewModel>(parameter, this);

            return viewModel;
        }

        private async Task<Result> FetchProductAttributesAsync(int movementId)
        {
            productAttributes = await WebClient.ExecuteApiRequestAsync(new QueryMovementProductAttributes(movementId));

            productParentCategories = productAttributes.ToDictionary(x => x.ProductId, x => x.ParentCategoryId);

            accountingSystemSerials = productAttributes.ToDictionary(x => x.ProductId, x => x.Serials);

            return Result.Success();
        }

        private async Task<Result> FetchAssemblyServicesAsync()
        {
            AssemblyServicesFilteringItem filteringItem = new AssemblyServicesFilteringItem()
            {
                SubdivisionId = Subdivision.Telemart.Id,
                StateIds = string.Join(",", new[] { AssemblyServiceState.Completed.Id }),
                OrderStateIds = string.Join(",", new[] { OrderStatus.Received.Id, OrderStatus.Confirmed.Id, OrderStatus.Packed.Id })
            };

            PagedResult<AssemblyServiceDto> assemblyServices = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filteringItem));

            _nomenclatureSeriesWithOrders = assemblyServices.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.NomenclatureSeries))
                .DistinctBy(x => x.NomenclatureSeries)
                .ToDictionary(x => x.NomenclatureSeries, x => x.OrderId);

            return Result.Success();
        }

        private IEnumerable<MovementProductViewItem> GetScanGroupItems(MovementProductViewItem viewItem)
        {
            if (viewItem.AssemblyServiceId.HasValue)
            {
                foreach (MovementProductViewItem assemblyServiceProductViewItem in Model.MovementProducts.Where(x => x.AssemblyServiceId == viewItem.AssemblyServiceId))
                {
                    yield return assemblyServiceProductViewItem;
                }

                yield break;
            }

            if (viewItem.IsConsumableAdditionalServiceProduct)
            {
                MovementProductViewItem additionalServiceProduct = Model.MovementProducts.First(x => x.RowRef == viewItem.ParentRowRef);
                MovementProductViewItem product = Model.MovementProducts.First(x => x.RowRef == additionalServiceProduct.ParentRowRef);

                yield return additionalServiceProduct;
                yield return product;

                IEnumerable<MovementProductViewItem> allConsumables = Model.MovementProducts
                    .Where(x => x.ParentRowRef == additionalServiceProduct.RowRef);

                foreach (MovementProductViewItem consumable in allConsumables)
                {
                    yield return consumable;
                }

                yield break;
            }

            if (viewItem.IsAdditionalServiceProduct)
            {
                MovementProductViewItem parentProduct = Model.MovementProducts.First(x => x.RowRef == viewItem.ParentRowRef);
                MovementProductViewItem[] consumableAdditionalServiceProducts = Model.MovementProducts
                    .Where(x => x.ParentRowRef == viewItem.RowRef && x.IsConsumableAdditionalServiceProduct)
                    .ToArray();

                foreach (MovementProductViewItem consumableAdditionalServiceProduct in consumableAdditionalServiceProducts)
                {
                    yield return consumableAdditionalServiceProduct;
                }

                yield return parentProduct;
                yield return viewItem;
                yield break;
            }

            MovementProductViewItem childAdditionalServiceProduct = Model.MovementProducts
                .FirstOrDefault(x => x.ParentRowRef == viewItem.RowRef && x.IsAdditionalServiceProduct);

            if (childAdditionalServiceProduct is not null)
            {
                yield return childAdditionalServiceProduct;
                yield return viewItem;

                MovementProductViewItem[] consumableAdditionalServiceProducts = Model.MovementProducts
                    .Where(x => x.ParentRowRef == childAdditionalServiceProduct.RowRef && x.IsConsumableAdditionalServiceProduct)
                    .ToArray();

                foreach (MovementProductViewItem consumableAdditionalServiceProduct in consumableAdditionalServiceProducts)
                {
                    yield return consumableAdditionalServiceProduct;
                }

                yield break;
            }

            yield return viewItem;
        }

        private decimal GetTotalWeightFact()
        {
            return (decimal)Model.MovementProducts.Sum(x => x.Weight * x.QuantityOut);
        }

        private decimal GetTotalWeightPlan()
        {
            return (decimal)Model.MovementProducts.Sum(x => x.Weight * x.Quantity);
        }
    }
}