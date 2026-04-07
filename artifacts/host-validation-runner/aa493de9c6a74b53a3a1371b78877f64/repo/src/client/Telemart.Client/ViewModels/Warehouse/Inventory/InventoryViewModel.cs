using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Inventory;
using Telemart.Client.Data.Requests.Features.Inventory.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Inventory;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    internal sealed class InventoryViewModel : TelemartDialogViewModelBase
    {
        private readonly RecognizeBarcodeSettings settings = new(true, true, allowOurAssemblyService: true);
        private IReadOnlyCollection<CategoryViewItem> _categories;

        public InventoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            PrintingSettingsStore = printingSettingsStore ?? throw new ArgumentNullException(nameof(printingSettingsStore));

            ResetProductRealQuantityCommand = new DelegateCommand(ResetProductRealQuantity, () => SelectedInventoryProduct != null);
            LockCommand = new AsyncCommand(LockAsync);
            CompleteInventoryCommand = new AsyncCommand(CompleteAsync);
            ShowResultsCommand = new DelegateCommand(ShowResults);
            ExportToExcelCommand = new DelegateCommand<TableView>(ExportToExcel);
            PrintLeftoversCommand = new AsyncCommand(PrintLeftoversAsync, () => Inventory?.EmployeeLockId == null);
            SaveCommand = new AsyncCommand(SaveAsync, CanOk);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;
            RecognizeBarcodeViewModel.OnFinishCommand += RecognizeBarcodeViewModelOnFinishCommand;

            InventoriedProducts = new ObservableRangeCollection<InventoryProductViewItem>();
            Categories = new ObservableRangeCollection<CategoryViewItem>();
        }

        #region INPC

        public InventoryViewItem Inventory
        {
            get { return GetProperty(() => Inventory); }
            set { SetProperty(() => Inventory, value); }
        }

        public ObservableRangeCollection<InventoryProductViewItem> InventoriedProducts
        {
            get { return GetProperty(() => InventoriedProducts); }
            set { SetProperty(() => InventoriedProducts, value); }
        }

        public InventoryProductViewItem SelectedInventoryProduct
        {
            get { return GetProperty(() => SelectedInventoryProduct); }
            set { SetProperty(() => SelectedInventoryProduct, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ObservableRangeCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        #endregion

        #region Commands

        public IDelegateCommand ResetProductRealQuantityCommand { get; }

        public IAsyncCommand LockCommand { get; }

        public IAsyncCommand CompleteInventoryCommand { get; }

        public IAsyncCommand PrintLeftoversCommand { get; }

        public IDelegateCommand ShowResultsCommand { get; }

        public IDelegateCommand ExportToExcelCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        #endregion

        public override int Width => 920;

        public override int Height => 640;

        public override int MinWidth => 800;

        public override int MinHeight => 640;

        public bool IsLockedByCurrentEmployee => CurrentEmployee.Id == Inventory?.EmployeeLockId;

        public bool IsLocked => Inventory?.EmployeeLockId != null;

        public bool IsCompleted => Inventory == null || Inventory.InventoriedOn != null;

        public bool VisibleWithGroup => Inventory?.GroupInventories?.Any() == true;

        public bool VisibleIsCompletedWithGroup => IsCompleted && VisibleWithGroup;

        public string GroupHeaderNameFact => VisibleWithGroup ? "Факт (по группе)" : "Факт";

        public string GroupHeaderNamePlan => VisibleWithGroup ? "План (по группе)" : "План";

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; set; }

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private int InventoryId { get; set; }

        private Dictionary<int, ProductAttributesDto> ProductAttributesDictionary { get; set; }

        private Dictionary<int, WarehouseSimpleDto> WarehousesDictionary { get; set; }

        private Dictionary<int, string> EmployeesDictionary { get; set; }

        private EmployeeDto CurrentEmployee => WebClient.AuthenticatedEmployee;

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        public override void OnDestroy()
        {
            if (IsLockedByCurrentEmployee)
            {
                try
                {
                    LockResponse<InventoryDto> response = WebClient.ExecuteApiRequest(new UnlockInventory(Inventory.Id));

                    Messenger.Send(new InventoryMessage(response.Dto, MessageType.Changed));

                    if (response.Dto.GroupInventories != null)
                    {
                        foreach (InventoryDto groupInventory in response.Dto.GroupInventories)
                        {
                            Messenger.Send(new InventoryMessage(groupInventory, MessageType.Changed));
                        }
                    }

                    if (!response.Success)
                    {
                        MessageFacadeService.ShowNotificationError("Не удалось разблокировать документ");
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to unlock entity");
                    MessageFacadeService.ShowNotificationError("Ошибка при разблокировании");
                }
            }

            base.OnDestroy();
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            InventoryId = (int)parameter;
        }

        protected override async Task HandleLoadedAsync()
        {
            Task<InventoryDto> inventoryTask = WebClient.ExecuteApiRequestAsync(new QueryInventory(InventoryId));
            Task<List<WarehouseDto>> warehousesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            Task<List<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Task<List<CategoryDto>> categoriesTask = WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            await Task.WhenAll(inventoryTask, warehousesTask, employeesTask, categoriesTask);

            WarehousesDictionary = Mapper.Map<List<WarehouseSimpleDto>>(warehousesTask.Result).ToDictionary(x => x.Id);
            EmployeesDictionary = employeesTask.Result.ToDictionary(x => x.Id, x => x.Name);
            List<CategoryViewItem> categoriesTillParent = GetCategoriesTillParent(Mapper.Map<List<CategoryViewItem>>(categoriesTask.Result));

            _categories = categoriesTillParent;

            await InitInventoryAsync(inventoryTask.Result);
        }

        protected override async Task HandleOkAsync()
        {
            if (await SaveInventoryAsync())
            {
                CloseOk();
            }
        }

        protected override void HandleCancel()
        {
            if (IsLockedByCurrentEmployee)
            {
                if (MessageFacadeService.Confirm("Вы уверены, что хотите закрыть диалог?"))
                {
                    base.HandleCancel();
                }

                return;
            }

            base.HandleCancel();
        }

        private static List<CategoryViewItem> GetCategoriesTillParent(List<CategoryViewItem> categories)
        {
            List<CategoryViewItem> isParentCategories = categories.Where(x => x.IsParent).ToList();
            Dictionary<int, CategoryViewItem> categoriesDictionary = categories.ToDictionary(x => x.Id);
            List<CategoryViewItem> resultCategories = new List<CategoryViewItem>();

            foreach (CategoryViewItem categoryViewItem in isParentCategories)
            {
                resultCategories.Add(categoryViewItem);
                AddCategoryParents(categoryViewItem, categoriesDictionary, resultCategories);
            }

            return resultCategories;
        }

        private static void AddCategoryParents(CategoryViewItem childCategory, IReadOnlyDictionary<int, CategoryViewItem> categoriesDictionary, ICollection<CategoryViewItem> parentCategories)
        {
            HashSet<int> parentIdsHash = new HashSet<int>(parentCategories.Select(x => x.Id));
            int parentId = childCategory.ParentId;

            while (parentId != Constants.RootCategoryId)
            {
                categoriesDictionary.TryGetValue(parentId, out CategoryViewItem parentCategory);

                if (parentCategory == null)
                {
                    break;
                }

                if (parentIdsHash.Contains(parentId))
                {
                    parentId = parentCategory.ParentId;
                    continue;
                }

                parentCategories.Add(parentCategory);
                parentIdsHash.Add(parentId);
                parentId = parentCategory.ParentId;
            }
        }

        private static string GetDefaultFolderPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private static string GetFileName(InventoryViewItem inventory)
        {
            return $"Inventory_{inventory.Id}_{DateTime.Now:yyyy-MM-dd}";
        }

        private async Task<bool> SaveInventoryAsync()
        {
            try
            {
                InventoryQuantitiesSaveDto saveDto = new InventoryQuantitiesSaveDto
                {
                    InventoryId = Inventory.Id,
                    TransferScannedBalances = Inventory.TransferScannedBalances,
                    InventoryProductsQuantities = InventoriedProducts.Where(x => x.QuantityReal > 0).Select(x =>
                        new InventoryProductQuantityRealDto
                        {
                            InventoryProductId = x.Id,
                            QuantityReal = x.QuantityReal,
                            ProductId = x.ProductId
                        }).ToList()
                };

                await WebClient.ExecuteApiRequestAsync(new UpdateInventoryRealQuantities(saveDto));

                MessageFacadeService.ShowNotificationInfo($"Инвентаризация №{Inventory.Id} успешно сохранена");

                return true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving inventory");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении инвентаризации");
            }

            return false;
        }

        private async Task SaveAsync()
        {
            await SaveInventoryAsync();
        }

        private void ResetProductRealQuantity()
        {
            try
            {
                if (!MessageFacadeService.Confirm("Вы уверены, что хотите удалить продукт из инвентаризации?"))
                {
                    return;
                }

                InventoriedProducts.Remove(SelectedInventoryProduct);
                SelectedInventoryProduct = null;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while reseting inventory product quantity");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении товара в инвентаризации");
            }
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
        }

        private void RecognizeBarcodeViewModelOnFinishCommand(object sender, EventArgs e)
        {
        }

        private async void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            switch (e.Result)
            {
                case RecognizeBarcodeResult.FoundInSupplier:
                case RecognizeBarcodeResult.Found:
                {
                    int[] selectedCategoryIds = Categories.Where(x => x.Selected == true).Select(x => x.Id).ToArray();

                    if (selectedCategoryIds.Contains(e.Product.ParentCategoryId))
                    {
                        AddProduct(e.Product, e.Quantity);
                    }
                    else
                    {
                        CategoryViewItem categoryItem = Categories.FirstOrDefault(x => x.Id == e.Product.ParentCategoryId);

                        e.WarningAction = () => MessageFacadeService.ShowMessageBoxWarning(
                            $"Товар {e.Product.Name}({e.ProductId}) относится к категории {categoryItem?.Name}, которая не выбрана для сканирования в инвентаризационную ведомость",
                            "Внимание!");

                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "Товар не относится к выбранным категориям");
                    }
                    return;
                }
                case RecognizeBarcodeResult.FoundAssembly:
                {

                    if (InventoriedProducts.Any(x => x.AssemblyServiceId == e.AssemblyServiceId))
                    {
                        MessageFacadeService.ShowNotificationWarning("Сборка уже просканирована");
                        return;
                    }

                    try
                    {
                        AssemblyServiceDto assemblyService =
                            await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(e.AssemblyServiceId));

                        if (assemblyService.StateId == AssemblyServiceState.Waiting.Id ||
                            assemblyService.StateId == AssemblyServiceState.Waiting.Id ||
                            assemblyService.StateId == AssemblyServiceState.Waiting.Id)
                        {
                            MessageFacadeService.ShowNotificationError(
                                $"Сборка не должна быть в статусе '{Dictionaries.GetItemById<AssemblyServiceState>(assemblyService.StateId).Name}'");
                            return;
                        }

                        PagedResult<ProductAttributesDto> result = await WebClient.ExecuteApiRequestAsync(
                            new QueryProductsAttributesByIds(
                                assemblyService.Products.Select(x => x.ProductId).ToArray(),
                                false));
                        Dictionary<int, ProductAttributesDto> attributes = result.Data.ToDictionary(x => x.ProductId);

                        foreach (AssemblyServiceProductDto product in assemblyService.Products)
                        {
                            if (attributes.TryGetValue(product.ProductId, out ProductAttributesDto productAttributes))
                            {
                                AddProduct(productAttributes, product.Quantity);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при получении товаров сборки");
                        Logger.LogError(exception, "Failed to get products from assembly service");
                    }

                    return;
                }
                case RecognizeBarcodeResult.NotFound:
                {
                    e.Message = new RecognizeBarcodeMessage(RecognizeBarcodeMessageType.Warning, "Товар не найден");
                    return;
                }
            }
        }

        private void AddProduct(ProductAttributesDto product, int quantity)
        {
            InventoryProductViewItem item = GetProductItem(product, quantity, out bool isNewProduct);

            if (isNewProduct)
            {
                InventoriedProducts.Add(item);
            }
            else
            {
                item.QuantityReal += quantity;
            }

            SelectedInventoryProduct = item;
        }

        private async Task PrintLeftoversAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();

            splashScreenManager.Show();

            try
            {
                InventoryPrintDto printDto = await WebClient.ExecuteApiRequestAsync(new QueryInventoryPrint(InventoryId));

                LeftoversReportData reportData = new LeftoversReportData(
                    printDto.InventoryId,
                    printDto.EmployeeName,
                    printDto.WarehouseName,
                    printDto.Products
                        .GroupBy(x => x.ParentCategoryName)
                        .Where(x => x.Any())
                        .Select(x => new LeftoversCategoryReportData(x.Key, x.Select(z => new LeftoversProductReportData(z.ProductId, z.ProductName, z.Quantity, z.QuantityFree)).ToList())).ToList());

                IReport report = new LeftoversReport { DataSource = new[] { reportData } };

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
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }

            splashScreenManager.Close();
        }

        private InventoryProductViewItem GetProductItem(ProductAttributesDto recognizedProduct, int quantity, out bool isNew)
        {
            InventoryProductViewItem product = InventoriedProducts.FirstOrDefault(x => x.ProductId == recognizedProduct.ProductId);

            isNew = false;
            if (product == null)
            {
                product = InventoryProductViewItem.Create(
                    null,
                    Inventory.Id,
                    recognizedProduct.ProductId,
                    recognizedProduct.GetLocalName(LocalizableNameType.Ukr),
                    EntityLocalіzerExtensions.GetLacalString(recognizedProduct.ParentCategoryName, recognizedProduct.ParentCategoryNameUkr, recognizedProduct.ParentCategoryNameEn, LocalizableNameType.Ukr),
                    0,
                    quantity,
                    recognizedProduct.ParentCategoryId);
                isNew = true;
            }

            return product;
        }

        private void InitInventoryProduct(InventoryProductViewItem product)
        {
            ProductAttributesDictionary.TryGetValue(product.ProductId, out ProductAttributesDto attributes);
            if (attributes != null)
            {
                product.FullName = attributes.GetLocalName(LocalizableNameType.Ukr);
                product.ParentCategoryName = EntityLocalіzerExtensions.GetLacalString(
                    attributes.ParentCategoryName,
                    attributes.ParentCategoryNameUkr,
                    attributes.ParentCategoryNameEn,
                    LocalizableNameType.Ukr);
                product.ParentCategoryId = attributes.ParentCategoryId;
            }
        }

        private async Task LockAsync()
        {
            try
            {
                LockResponse<InventoryDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockInventory(Inventory.Id));

                if (lockResponse.Success)
                {
                    EmployeeSimpleDto employeeLock = new EmployeeSimpleDto
                    {
                        Id = CurrentEmployee.Id,
                        Login = CurrentEmployee.Login,
                        Name = CurrentEmployee.Name,
                        ShortName = CurrentEmployee.ShortName
                    };

                    Inventory.EmployeeLock = employeeLock;
                    Inventory.EmployeeLockId = employeeLock.Id;

                    RaisePropertiesChanged(nameof(IsLockedByCurrentEmployee), nameof(IsLocked));
                }
                else
                {
                    await InitInventoryAsync(lockResponse.Dto);
                    MessageFacadeService.ShowNotificationWarning($"Инвентаризация уже заблокирована пользователем {lockResponse.Dto.EmployeeLock.Name}");
                }

                Messenger.Send(new InventoryMessage(lockResponse.Dto, MessageType.Changed));

                if (lockResponse.Dto.GroupInventories != null)
                {
                    foreach (InventoryDto groupInventory in lockResponse.Dto.GroupInventories)
                    {
                        Messenger.Send(new InventoryMessage(groupInventory, MessageType.Changed));
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to initialize inventory edit");
                MessageFacadeService.ShowNotificationError("Ошибка при редактировании инвентаризации");
            }
        }

        private async Task InitInventoryAsync(InventoryDto inventory)
        {
            Inventory = Mapper.Map<InventoryViewItem>(inventory);

            SetAllCategoriesSelected(inventory);

            Inventory.Warehouse = WarehousesDictionary[inventory.WarehouseId];
            Inventory.CreatedByEmployeeName = EmployeesDictionary[inventory.CreatedByEmployeeId];

            foreach (InventoryViewItem groupInventory in Inventory.GroupInventories)
            {
                groupInventory.Warehouse = WarehousesDictionary[groupInventory.WarehouseId];
                groupInventory.CreatedByEmployeeName = EmployeesDictionary[groupInventory.CreatedByEmployeeId];
            }

            RaisePropertiesChanged(nameof(IsCompleted), nameof(VisibleWithGroup), nameof(VisibleIsCompletedWithGroup));
            RaisePropertiesChanged(nameof(GroupHeaderNamePlan), nameof(GroupHeaderNameFact));

            List<ProductAttributesDto> productAttributes = await WebClient.ExecuteApiRequestAsync(new QueryInventoryProductAttributes(Inventory.Id)) ?? new List<ProductAttributesDto>();

            foreach (InventoryViewItem groupInventory in Inventory.GroupInventories)
            {
                List<ProductAttributesDto> groupInventoryProductAttributes = await WebClient.ExecuteApiRequestAsync(new QueryInventoryProductAttributes(groupInventory.Id));

                if (groupInventoryProductAttributes?.Any() == true)
                {
                    productAttributes = productAttributes.Union(groupInventoryProductAttributes).ToList();
                }

                foreach (InventoryProductViewItem groupInventoryProduct in groupInventory.InventoryProducts)
                {
                    InventoryProductViewItem currentInventoryProduct = Inventory.InventoryProducts.FirstOrDefault(x => x.ProductId == groupInventoryProduct.ProductId);

                    if (currentInventoryProduct is null)
                    {
                        Inventory.InventoryProducts.Add(InventoryProductViewItem.Create(
                            groupInventoryProduct.Id,
                            Inventory.Id,
                            groupInventoryProduct.ProductId,
                            null,
                            null,
                            groupInventoryProduct.Quantity,
                            groupInventoryProduct.QuantityReal,
                            null));
                    }
                    else
                    {
                        currentInventoryProduct.Quantity += groupInventoryProduct.Quantity;
                        currentInventoryProduct.QuantityReal += groupInventoryProduct.QuantityReal;
                    }
                }
            }

            productAttributes = productAttributes.GroupBy(x => x.ProductId).Select(x => x.First()).ToList();

            ProductAttributesDictionary = productAttributes.ToDictionary(x => x.ProductId);

            foreach (InventoryProductViewItem inventoryProductViewItem in Inventory.InventoryProducts)
            {
                InitInventoryProduct(inventoryProductViewItem);
            }

            RecognizeBarcodeViewModel.Init(settings, productAttributes);

            InventoriedProducts.Clear();

            InventoriedProducts.AddRange(Inventory.InventoryProducts.Where(x => x.QuantityReal > 0).OrderBy(x => x.ParentCategoryName).ThenBy(x => x.FullName));

            Title = $"Инвентаризация №{InventoryId} от {Inventory.CreatedOn:dd.MM.yyyy HH:mm:ss}";

            RaisePropertiesChanged(nameof(InventoriedProducts), nameof(IsLockedByCurrentEmployee), nameof(IsLocked), nameof(Categories));

            SummaryItems = GetSummaryItems();
        }

        private async Task CompleteAsync()
        {
            try
            {
                if (!MessageFacadeService.Confirm("Вы уверены?"))
                {
                    return;
                }

                Result<InventoryDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteInventory(InventoryId));
                await InitInventoryAsync(result.Data);

                MessageFacadeService.ShowNotificationInfo($"Инвентаризация №{Inventory.Id} успешно закрыта");
                Messenger.Send(new InventoryMessage(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при завершении инвентаризации", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при завершении инвентаризации", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while completing inventorization");
                MessageFacadeService.ShowNotificationError("Ошибка при завершении инвентаризации");
            }
        }

        private List<ProductComparisonResult> GetDeviation()
        {
            HashSet<int> existingIds = new HashSet<int>(Inventory.InventoryProducts.Select(x => x.ProductId));
            Dictionary<int, InventoryProductViewItem> inventoriedProducts = InventoriedProducts.ToDictionary(x => x.ProductId);
            List<ProductComparisonResult> items = new List<ProductComparisonResult>();

            foreach (InventoryProductViewItem inventoryProductViewItem in Inventory.InventoryProducts)
            {
                if (Inventory.TransferScannedBalances && inventoryProductViewItem.CurrentInventoryQuantityReal <= 0)
                {
                    continue;
                }

                inventoriedProducts.TryGetValue(inventoryProductViewItem.ProductId, out InventoryProductViewItem inventoriedItem);

                ProductComparisonResult comparisonResult = new ProductComparisonResult(inventoryProductViewItem.FullName, inventoryProductViewItem.CurrentInventoryQuantity, inventoriedItem?.CurrentInventoryQuantityReal ?? 0, inventoryProductViewItem.ProductId);

                if (comparisonResult.DeviationQuantity == 0)
                {
                    continue;
                }

                items.Add(comparisonResult);
            }

            foreach (InventoryProductViewItem inventoryProduct in InventoriedProducts.Where(x => !existingIds.Contains(x.ProductId)))
            {
                if (Inventory.TransferScannedBalances && inventoryProduct.CurrentInventoryQuantityReal <= 0)
                {
                    continue;
                }

                ProductComparisonResult comparisonResult = new ProductComparisonResult(inventoryProduct.FullName, inventoryProduct.CurrentInventoryQuantity, inventoryProduct.CurrentInventoryQuantityReal);
                if (comparisonResult.DeviationQuantity == 0)
                {
                    continue;
                }

                items.Add(comparisonResult);
            }

            return items;
        }

        private void ShowResults()
        {
            List<ProductComparisonResult> items = GetDeviation();

            if (items.Any())
            {
                ProductComparisonResultViewModel validationViewModel = DialogDocumentManagerService.ShowView<ProductComparisonResultViewModel>(new object[] { items, false }, this);

                if (!validationViewModel.IsOk)
                {
                    return;
                }
            }

            MessageFacadeService.ShowNotificationInfo("Инвентаризация проведена успешно. Расхождений нет");
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Номер", string.Join(",", new[] { Inventory.Id }.Union(Inventory.GroupInventories.Select(x => x.Id))));

            yield return new SummaryViewItem("Создал", Inventory.CreatedByEmployeeName);

            yield return new SummaryViewItem("Создан", Inventory.CreatedOn.ToString("dd.MM HH:mm"));

            yield return new SummaryViewItem("Склады", string.Join(",\n", new[] { $"{Inventory.Warehouse.Name}" }.Union(Inventory.GroupInventories.Select(x => x.Warehouse.Name))));

            if (!string.IsNullOrEmpty(Inventory.Comment))
            {
                yield return new SummaryViewItem("Комментарий", Inventory.Comment);
            }

            yield return new SummaryViewItem("Отск. товары в 1С", BoolToStr(Inventory.TransferScannedBalances));

            static string BoolToStr(bool value)
            {
                return value ? SignalsConstants.TrueStr : SignalsConstants.FalseStr;
            }
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
                GetDefaultFolderPath(),
                GetFileName(Inventory));
        }

        private void SetAllCategoriesSelected(InventoryDto inventory)
        {
            Categories.Clear();

            if (_categories is null)
            {
                return;
            }

            int[] inventoryProductParentCategoryIds = Inventory.InventoryProducts.Where(x => x.ParentCategoryId.HasValue).Select(x => x.ParentCategoryId.Value).Distinct().ToArray();

            foreach (var categoryViewItem in _categories)
            {
                bool? isSelected = inventory.CategoryIds?.Any() == true
                    ? inventory.CategoryIds.Contains(categoryViewItem.Id)
                    : inventoryProductParentCategoryIds.Contains(categoryViewItem.Id);

                if (isSelected == false && IsChiledSelected(categoryViewItem, inventory.CategoryIds))
                {
                    isSelected = null;
                }

                categoryViewItem.Selected = isSelected;
            }

            Categories.AddRange(_categories.OrderBy(x => x.Position).ThenBy(x => x.Name).ToArray());
        }

        private bool IsChiledSelected(CategoryViewItem item, int[] selectedCategory)
        {
            if (selectedCategory.Contains(item.Id))
            {
                return true;
            }

            CategoryViewItem[] chiledItems = _categories.Where(x => x.ParentId == item.Id).ToArray();

            if (chiledItems.Any())
            {
                foreach (CategoryViewItem currentItem in chiledItems)
                {
                    bool resilt = IsChiledSelected(currentItem, selectedCategory);

                    if (resilt)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}