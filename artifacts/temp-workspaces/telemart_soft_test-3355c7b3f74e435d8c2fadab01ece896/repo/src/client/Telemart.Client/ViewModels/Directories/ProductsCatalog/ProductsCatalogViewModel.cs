using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.Products.Catalog;
using Telemart.Client.Data.Requests.Features.Products.Colors;
using Telemart.Client.Data.Requests.Features.Warranty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Directories.ProductsCatalog
{
    public sealed class ProductsCatalogViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private const int MaxProductsCount = 20000;
        private readonly List<IAsyncCommand> asyncCommands;
        private IReadOnlyDictionary<int, CategoryProductCatalogViewItem> categoriesDictionary;
        private Dictionary<int, int[]> categoryFeatures;
        private List<CategoryProductCatalogViewItem> categoryItems;
        private IReadOnlyDictionary<int, ProductColorViewItem> productColorsDictionary;
        private IReadOnlyDictionary<string, ProductColorViewItem> productColorsDictionaryByName;
        private CategoryProductCatalogViewItem mbtCategory;
        private int filterCounter;
        private CategoryViewItem loadedCategory;

        public ProductsCatalogViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IExcelImportSettingsEngineAsync<ProductCatalogImportItem, ProductsCatalogExcelImportSettings> excelImportEngine,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ExcelImportEngine = excelImportEngine ?? throw new ArgumentNullException(nameof(excelImportEngine));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AddCommand = new DelegateCommand(Add);
            CloneCommand = new DelegateCommand(Clone, () => SelectedProduct != null);
            ResetCommand = new DelegateCommand<ProductCatalogViewItemWrapper>(Reset, x => x != null);
            RemoveCommand = new DelegateCommand<ProductCatalogViewItemWrapper>(Remove);
            RefreshCommand = new AsyncCommand(RefreshAsync, () => Filter?.SelectedCategory != null);
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            SaveCommand = new AsyncCommand(SaveAsync);
            ExportCommand = new DelegateCommand<TableView>(Export);
            ImportCommand = new AsyncCommand(ImportAsync);
            HandleShowingEditorCommand = new DelegateCommand<ShowingEditorEventArgs>(HandleShowingEditor);
            HandleTableViewLoadedCommand = new DelegateCommand<RoutedEventArgs>(HandleTableViewLoaded);
            SetFilterTypeCommand = new DelegateCommand<ProductsCatalogFilterType>(SetFilterType);
            GetCategoryCommand = new DelegateCommand(GetCategory);

            Products = new ObservableRangeCollection<ProductCatalogViewItemWrapper>();
            ProductsView = new ObservableRangeCollection<ProductCatalogViewItemWrapper>();

            Filter = new ProductsCatalogFilterViewModel(webClient, dictionaries, mapper);

            asyncCommands = new List<IAsyncCommand>
            {
                RefreshCommand,
                SaveCommand
            };

            AllowSorting = true;
            IsAllowEditProductNameUkr = WebClient.IsOperationAllowed(BusinessOperation.ProductEditUkrName);
        }

        public ProductsCatalogViewModel()
        {
        }

        #region INPC

        public ProductsCatalogFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ObservableRangeCollection<ProductCatalogViewItemWrapper> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<WarrantyType> WarrantyTypes
        {
            get { return GetProperty(() => WarrantyTypes); }
            private set { SetProperty(() => WarrantyTypes, value); }
        }

        public ReadOnlyObservableCollection<ProductType> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public ObservableRangeCollection<ProductCatalogViewItemWrapper> ProductsView
        {
            get { return GetProperty(() => ProductsView); }
            private set { SetProperty(() => ProductsView, value); }
        }

        public ProductCatalogViewItemWrapper SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, () => RaisePropertyChanged(nameof(Features))); }
        }

        public ReadOnlyObservableCollection<Warranty> Warranties
        {
            get { return GetProperty(() => Warranties); }
            private set { SetProperty(() => Warranties, value); }
        }

        public ObservableCollection<ProductColorViewItem> ProductColors
        {
            get { return GetProperty(() => ProductColors); }
            set { SetProperty(() => ProductColors, value); }
        }

        public ReadOnlyObservableCollection<FeatureDto> AllFeatures
        {
            get { return GetProperty(() => AllFeatures); }
            set { SetProperty(() => AllFeatures, value); }
        }

        public IEnumerable<FeatureDto> Features => SelectedProduct != null && categoryFeatures.TryGetValue(SelectedProduct.CategoryId, out int[] featureIds)
            ? AllFeatures.Where(x => featureIds.Contains(x.Id))
            : Array.Empty<FeatureDto>();

        public ProductsCatalogFilterType FilterType
        {
            get { return GetProperty(() => FilterType); }
            set { SetProperty(() => FilterType, value, () => FilterProductsCollection(FilterType)); }
        }

        public bool AllowSorting
        {
            get { return GetProperty(() => AllowSorting); }
            set { SetProperty(() => AllowSorting, value); }
        }

        public TableView TableView
        {
            get { return GetProperty(() => TableView); }
            set { SetProperty(() => TableView, value); }
        }

        public bool IsAllowEditProductNameAnyTime => WebClient.IsOperationAllowed(BusinessOperation.ProductCatalogEditAnyTime);

        public bool IsAllowEditProductNameUkr { get; }

        #endregion

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand CloneCommand { get; }

        public IDelegateCommand ResetCommand { get; }

        public IDelegateCommand RemoveCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IAsyncCommand ImportCommand { get; }

        public IDelegateCommand HandleShowingEditorCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ResetFilterCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand HandleTableViewLoadedCommand { get; }

        public IDelegateCommand SetFilterTypeCommand { get; }

        public IDelegateCommand GetCategoryCommand { get; }

        #endregion

        private IExcelImportSettingsEngineAsync<ProductCatalogImportItem, ProductsCatalogExcelImportSettings> ExcelImportEngine { get; }

        private IMapper Mapper { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>(
            "SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>(
            "DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>(
            "ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>(
            "ImportFromExcelFileDialogService");

        public bool HandleHotkey(HotkeyMessage msg)
        {
            if (asyncCommands.Any(x => x.IsExecuting))
            {
                return false;
            }

            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Control)
            {
                switch (msg.Key)
                {
                    case Key.S:
                        SaveCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.Insert:
                        CloneCommand.Execute(SelectedProduct);
                        handled = true;
                        break;
                    case Key.E:
                        ExportCommand.Execute(TableView);
                        handled = true;
                        break;
                    case Key.F:
                        FilterType = (ProductsCatalogFilterType)(++filterCounter % 3);
                        handled = true;
                        break;
                    case Key.R:
                        ResetCommand.Execute(SelectedProduct);
                        handled = true;
                        break;
                    case Key.D:
                        RemoveCommand.Execute(SelectedProduct);
                        handled = true;
                        break;
                    case Key.I:
                        ImportCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.Key)
                {
                    case Key.F5:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.F6:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        break;
                    case Key.Insert:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            if (IsInDesignMode)
            {
                return;
            }

            await Filter.RefreshAsync();
            Filter.SelectedCategory = Filter.Categories.First();

            RefreshWarranties();

            await Task.WhenAll(
                RefreshCategoriesAsync(),
                RefreshColorsAsync());

            await RefreshFeaturesAsync();
        }

        private static void ResetProductsChanges(List<ProductCatalogViewItemWrapper> editedItems)
        {
            foreach (ProductCatalogViewItemWrapper viewItemWrapper in editedItems)
            {
                viewItemWrapper.ResetToParentCategory();
            }
        }

        private async Task RefreshAsync()
        {
            if (Products.Any(x => x.IsEdited)
                && !MessageFacadeService.Confirm("Вы действительно хотите обновить данные?"))
            {
                return;
            }

            if (Filter.SelectedCategory == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите категорию");
                return;
            }

            Products.Clear();
            ProductsView.Clear();

            try
            {
                await Task.WhenAll(
                    Filter.RefreshAsync(),
                    RefreshCategoriesAsync(),
                    RefreshColorsAsync());

                await RefreshFeaturesAsync();

                ProductCatalogFilteringItem filteringItem = Filter.GetFilteringItem();

                PagedResult<ProductCatalogDto> products =
                    await WebClient.ExecuteApiRequestAsync(new QueryProductsCatalog(filteringItem));

                Products.AddRange(products.Data.Select(MapTransferObjectToViewItem));
                ProductsView.AddRange(Products);

                FilterProductsCollection(FilterType);

                loadedCategory = Filter.SelectedCategory;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh products catalog");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshCategoriesAsync()
        {
            const int mbtCategoryId = 1859;

            List<CategoryFullDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategoriesFull(), true);
            categoryItems = categories.Select(Mapper.Map<CategoryProductCatalogViewItem>).ToList();
            categoriesDictionary = categoryItems.ToDictionary(x => x.Id);
            categoriesDictionary.TryGetValue(mbtCategoryId, out mbtCategory);
        }

        private void RefreshWarranties()
        {
            WarrantyTypes = Dictionaries.GetItems<WarrantyType>().ToReadOnlyObservableCollection();

            ProductTypes = Dictionaries.GetItems<ProductType>().Where(x => !x.IsVirtual).ToReadOnlyObservableCollection();

            Warranties = Dictionaries.GetItems<Warranty>()
                .OrderBy(x => x.Weight)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshFeaturesAsync()
        {
            List<FeatureGroupDto> featureGroups = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups()).GetPagedResultDataAsync();

            categoryFeatures = new Dictionary<int, int[]>();

            foreach (IGrouping<int, FeatureGroupDto> featureGroupDtos in featureGroups.GroupBy(x => x.CategoryId))
            {
                int[] featureIds = featureGroupDtos.SelectMany(x => x.Features.Select(y => y.Id)).ToArray();

                CategoryProductCatalogViewItem category = categoryItems.FirstOrDefault(x => x.Id == featureGroupDtos.Key);

                if (category != null)
                {
                    foreach (CategoryProductCatalogViewItem catalogViewItem in categoryItems.Where(x => x.Left >= category.Left && x.Left <= category.Right))
                    {
                        categoryFeatures.Add(catalogViewItem.Id, featureIds);
                    }
                }
            }

            AllFeatures = featureGroups.SelectMany(x => x.Features).ToReadOnlyObservableCollection();
        }

        private async Task RefreshColorsAsync()
        {
            List<ProductColorDto> colors = await WebClient.ExecuteApiRequestAsync(new QueryProductColors(), true)
                .GetPagedResultDataAsync();
            ProductColors = Mapper.Map<List<ProductColorViewItem>>(colors)
                .OrderBy(x => x.Name)
                .ToObservableCollection();
            productColorsDictionary = ProductColors.ToDictionary(x => x.Id);
            productColorsDictionaryByName = ProductColors.ToDictionary(x => x.Name.ToLower());
        }

        private async Task SaveAsync()
        {
            ProgressScreenViewModel progressViewModel = null;

            try
            {
                List<ProductCatalogViewItemWrapper> editedItems = ProductsView.Where(x => x.IsEdited).ToList();

                if (!editedItems.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                    return;
                }

                if (editedItems.Count > MaxProductsCount)
                {
                    MessageFacadeService.ShowNotificationWarning(
                        $"Нельзя сохранять более чем {MaxProductsCount} товаров за раз");
                    return;
                }

                string[] duplicateFeatureGroupNames = ProductsView
                    .Where(x => !string.IsNullOrWhiteSpace(x.GroupName))
                    .GroupBy(x => x.GroupName)
                    .Where(x => x.GroupBy(y => y.GroupFeatureId).Count() > 1)
                    .Select(x => x.Key)
                    .ToArray();

                if (duplicateFeatureGroupNames.Any())
                {
                    MessageFacadeService.ShowNotificationWarning(
                        $"Для групп \"{string.Join(", ", duplicateFeatureGroupNames)}\" указаны неидентичные характеристики");
                    return;
                }

                if (editedItems.Any(x => IDataErrorInfoHelper.HasErrors(x)))
                {
                    MessageFacadeService.ShowNotificationWarning("В сохраняемых товарах есть ошибки");
                    return;
                }

                FilterType = ProductsCatalogFilterType.Changed;

                if (!MessageFacadeService.Confirm($"Будет сохранено {editedItems.Count} тов., продолжить?"))
                {
                    return;
                }

                GuidValuePair<ProductCatalogSaveDto>[] toSave = editedItems
                    .Select(
                        x => new GuidValuePair<ProductCatalogSaveDto>
                        {
                            Key = x.Guid,
                            Value = Mapper.Map<ProductCatalogSaveDto>(x)
                        })
                    .ToArray();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                progressViewModel = new ProgressScreenViewModel("Сохранение товаров", toSave.Length);

                Task task = SaveProductsInternalAsync(toSave, progressViewModel, cancellationTokenSource.Token);

                DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

                if (!progressViewModel.IsOk)
                {
                    cancellationTokenSource.Cancel();
                }

                await task;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save products");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");

                if (progressViewModel?.ProcessedCount > 0)
                {
                    MessageFacadeService.ShowNotificationWarning(
                        $"На данный момент уже было сохранено {progressViewModel.ProcessedCount} {WordEndingHelper.GetWordByNumber(progressViewModel.ProcessedCount, new[] { "товар", "товара", "товаров" })}");
                }
            }
            finally
            {
                progressViewModel?.CancelCommand.Execute(null);
            }
        }

        private async Task SaveProductsInternalAsync(
            GuidValuePair<ProductCatalogSaveDto>[] toSave,
            ProgressScreenViewModel progressViewModel,
            CancellationToken cancellationToken)
        {
            int savedCount = 0;
            bool hasErrors = false;

            foreach (GuidValuePair<ProductCatalogSaveDto>[] productsChunkToSave in toSave.Chunk(50))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                ProductsCatalogSaveRequest request = new ProductsCatalogSaveRequest
                    { Catalog = productsChunkToSave };

                ProductsCatalogSaveResponse result =
                    await WebClient.ExecuteApiRequestAsync(new UpdateProductsCatalog(request));

                Dictionary<Guid, GuidValuePair<ProductCatalogSaveResultDto>> savedItemsDictionary =
                    result.SaveResults.ToDictionary(x => x.Key);

                for (int i = 0; i < ProductsView.Count; i++)
                {
                    ProductCatalogViewItemWrapper existingViewItem = ProductsView[i];

                    savedItemsDictionary.TryGetValue(
                        existingViewItem.Guid,
                        out GuidValuePair<ProductCatalogSaveResultDto> pair);

                    if (pair == null || !pair.Value.Success || !string.IsNullOrEmpty(pair.Value.Error))
                    {
                        continue;
                    }

                    ProductCatalogViewItemWrapper newViewItem = MapTransferObjectToViewItem(pair.Value.Product);

                    int existingIndex = Products.IndexOf(existingViewItem);

                    if (existingIndex >= 0)
                    {
                        ProductsView[i] = newViewItem;
                        Products[existingIndex] = newViewItem;
                    }
                }

                ValidationResultItem[] validationErrors = result
                    .SaveResults
                    .Where(x => !x.Value.Success)
                    .Select(x => new ValidationResultItem(x.Value.Error, true))
                    .ToArray();

                if (validationErrors.Any())
                {
                    ShowValidationResultView("Ошибки при сохранении", validationErrors);
                    hasErrors = true;
                    break;
                }

                savedCount += result.SaveResults.Count(x => x.Value.Success);

                progressViewModel.SetProcessedCount(savedCount);
            }

            if (!hasErrors)
            {
                FilterType = ProductsCatalogFilterType.All;
            }

            if (savedCount > 0)
            {
                MessageFacadeService.ShowNotificationInfo(
                    $"Всего сохранено {savedCount} {WordEndingHelper.GetWordByNumber(savedCount, new[] { "товар", "товара", "товаров" })}");
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<ValidationResultItem> validationItems)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);
        }

        private ProductCatalogViewItemWrapper MapTransferObjectToViewItem(ProductCatalogDto source)
        {
            ProductCatalogViewItem item = Mapper.Map<ProductCatalogViewItem>(source);
            item.ColorPrimary = productColorsDictionary.GetValueOrDefault(item.ColorPrimaryId ?? -1, null);
            item.ColorSecondary = productColorsDictionary.GetValueOrDefault(item.ColorSecondaryId ?? -1, null);

            ProductCatalogViewItemWrapper itemWrapper = GetViewItemWrapper(item);

            return itemWrapper;
        }

        private ProductCatalogViewItemWrapper MapImportResultItemToViewItem(
            ProductCatalogImportItem result)
        {
            ProductCatalogViewItem item =
                Mapper.Map<ProductCatalogViewItem>(result.Dto ?? new ProductCatalogDto());
            bool isNew = result.Dto == null;
            if (isNew)
            {
                item.Name = result.Name;
                item.NameUkr = result.NameUkr;
                item.NameEn = result.NameEn;
                item.CategoryId = result.CategoryId;
                item.CreatedOn = DateTime.Now;
            }

            item.ColorPrimary = productColorsDictionary.GetValueOrDefault(item.ColorPrimaryId ?? -1, null);
            item.ColorSecondary = productColorsDictionary.GetValueOrDefault(item.ColorSecondaryId ?? -1, null);

            ProductCatalogViewItemWrapper itemWrapper = GetViewItemWrapper(item);

            AssignImportItemToItemWrapper();

            return itemWrapper;

            void AssignImportItemToItemWrapper()
            {
                itemWrapper = Mapper.Map(result, itemWrapper);

                if (!isNew && (IsAllowEditProductNameAnyTime || !ProductHelper.IsProductEditingOvertimed(result.Dto.CreatedOn)))
                {
                    itemWrapper.Name = result.Name;
                }

                itemWrapper.ResetToParentCategoryIfEmpty();

                itemWrapper.ColorPrimary =
                    productColorsDictionaryByName.GetValueOrDefault((result.ColorPrimary ?? string.Empty)
                        .ToLower());
                itemWrapper.ColorSecondary =
                    productColorsDictionaryByName.GetValueOrDefault((result.ColorSecondary ?? string.Empty)
                        .ToLower());
            }
        }

        private ProductCatalogViewItemWrapper GetViewItemWrapper(ProductCatalogViewItem viewItem)
        {
            ProductCatalogViewItemWrapper itemWrapper = Mapper.Map<ProductCatalogViewItemWrapper>(viewItem);

            itemWrapper.SetSupportedFeatures(categoryFeatures.AsReadOnly().GetValueOrDefault(itemWrapper.CategoryId));
            itemWrapper.SetCategory(categoriesDictionary, mbtCategory);
            itemWrapper.Initialize();

            return itemWrapper;
        }

        private void FilterProductsCollection(ProductsCatalogFilterType filterType)
        {
            IEnumerable<ProductCatalogViewItemWrapper> items = filterType == ProductsCatalogFilterType.Changed
                ? Products.Where(x => x.IsEdited)
                : filterType == ProductsCatalogFilterType.WithErrors
                    ? Products.Where(x => IDataErrorInfoHelper.HasErrors(x))
                    : Products;

            ProductCatalogViewItemWrapper selectedItem = SelectedProduct;

            ProductsView.Clear();
            ProductsView.AddRange(items);

            if (selectedItem != null)
            {
                SelectedProduct =
                    ProductsView.FirstOrDefault(x => (x.Id == 0 && x.Guid == selectedItem.Guid) ||
                                                     (x.Id != 0 && x.Id == selectedItem.Id));
            }
        }

        private void Export(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string selectedCategoryName = Filter.SelectedCategory == null
                ? string.Empty
                : $"_{Filter.SelectedCategory.Name}";

            string fileName = $"Products_Catalog{selectedCategoryName}_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                x =>
                {
                    List<ColumnBase> columnChooserColumns = new List<ColumnBase>(tableView.ColumnChooserColumns);

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = true;
                    }

                    string filePath = SaveFileDialogService.File.GetFullName();

                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = false;
                    }
                },
                folderPath,
                fileName);
        }

        private void HandleTableViewLoaded(RoutedEventArgs args)
        {
            TableView = args.Source as TableView;
        }

        private void ResetFilter()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void HandleShowingEditor(ShowingEditorEventArgs args)
        {
            ProductCatalogViewItemWrapper viewItemWrapper = (ProductCatalogViewItemWrapper)args.Row;

            if (!IsAllowEditProductNameAnyTime && viewItemWrapper.IsEditingOvertimed && args.Column.FieldName == nameof(viewItemWrapper.Name))
            {
                args.Cancel = true;
                MessageFacadeService.ShowNotificationWarning(
                    $"Нельзя редактировать название товарам старше {ProductHelper.ProductEditingOvertimeDays} дней");
                return;
            }

            if (!IsAllowEditProductNameUkr && args.Column.FieldName == nameof(viewItemWrapper.NameUkr) && !viewItemWrapper.IsNew)
            {
                args.Cancel = true;
                MessageFacadeService.ShowNotificationWarning("Нельзя редактировать украинское название товарам");
            }
        }

        private void Add()
        {
            ProductCatalogViewItemWrapper item = null;

            if (SelectedProduct is null)
            {
                if (Filter.SelectedCategory != null)
                {
                    CategoryProductCatalogViewItem category = categoriesDictionary[Filter.SelectedCategory.Id];

                    item = new ProductCatalogViewItemWrapper(new ProductCatalogViewItem())
                    {
                        Active = 1
                    };

                    item.SetCategory(category);
                    item.SetCategory(categoriesDictionary, mbtCategory);

                    item.SetSupportedFeatures(categoryFeatures.AsReadOnly().GetValueOrDefault(category.Id));

                    item.Initialize();

                    item.CreatedOn = DateTime.Now;
                    item.Id = 0;

                    item.ResetToParentCategory();
                }
            }
            else
            {
                item = SelectedProduct.CopyFromParentCategory();
            }

            if (item != null)
            {
                InsertAfterSelected(item);
            }
        }

        private void Clone()
        {
            ProductCatalogViewItemWrapper clone = SelectedProduct.Clone();
            InsertAfterSelected(clone);
        }

        private void Remove(ProductCatalogViewItemWrapper item)
        {
            ProductsView.Remove(item);
            Products.Remove(item);
        }

        private void Reset(ProductCatalogViewItemWrapper item)
        {
            item.ResetToParentCategory();
        }

        private void InsertAfterSelected(ProductCatalogViewItemWrapper item)
        {
            int index = ProductsView.IndexOf(SelectedProduct);

            AllowSorting =
                false; // Sorting of the columns will be cleared in order to place new item next to selected one
            ProductsView.Insert(index + 1, item);
            Products.Insert(index + 1, item);
            AllowSorting = true;
        }

        private async Task ImportAsync()
        {
            const int MaxFileSizeInMb = 20;

            List<ProductCatalogViewItemWrapper> editedItems = ProductsView.Where(x => x.IsEdited).ToList();

            if (editedItems.Any())
            {
                if (!MessageFacadeService.Confirm("Все текущие изменения будут отменены, продолжить?"))
                {
                    return;
                }

                ResetProductsChanges(editedItems);
            }

            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return;
            }

            if (OpenFileDialogService.File.Length > MaxFileSizeInMb.Megabytes().Bytes)
            {
                MessageFacadeService.ShowNotificationWarning($"Размер файла должен быть меньше чем {MaxFileSizeInMb} Мб");
                return;
            }

            string fileName = OpenFileDialogService.GetFullFileName();

            try
            {
                Dictionary<string, string> allowedColumns = TableView.Grid.Columns.Where(x => x.AllowPrinting && !string.IsNullOrEmpty(x.Name))
                    .ToDictionary(x => x.Name, y => y.Header as string);
                Dictionary<string, int> availableWarranties = (await WebClient.ExecuteApiRequestAsync(new QueryWarranties())
                    .GetPagedResultDataAsync()).ToDictionary(x => x.Name, y => y.Id);

                ProductsCatalogExcelImportSettings settings = new ProductsCatalogExcelImportSettings(
                    allowedColumns,
                    availableWarranties,
                    categoriesDictionary.Keys.ToHashSet(),
                    productColorsDictionaryByName.Keys.ToHashSet(),
                    WarrantyTypes.DistinctBy(x => x.Name).ToDictionary(x => x.Name, x => x.Id),
                    ProductTypes.DistinctBy(x => x.Name).ToDictionary(x => x.Name, x => x.Id),
                    MaxProductsCount);

                ExcelImportResult<ProductCatalogImportItem> result = await ExcelImportEngine.ImportFromXlsxAsync(fileName, settings);

                if (result.IsSuccess)
                {
                    Filter.Clear();
                    ProductsView.Clear();
                    Products.Clear();

                    List<ProductCatalogViewItemWrapper> resultingItems = result.ResultItems.Select(MapImportResultItemToViewItem).ToList();
                    ProductsView.AddRange(resultingItems);
                    Products.AddRange(resultingItems);

                    FilterType = ProductsCatalogFilterType.Changed;
                    FilterProductsCollection(FilterType);

                    MessageFacadeService.ShowNotificationInfo(
                        $"{resultingItems.Count} Товаров успешно импортировано. Вы можете отредактировать их сейчас, затем сохранить.");

                    List<ValidationResultItem> changedCategoryWarnings = result.ResultItems
                        .Where(x => x.Dto != null && x.CategoryId != x.Dto.CategoryId)
                        .Select(
                            x => new ValidationResultItem(
                                $"{x.Name}: Категория {x.Dto.CategoryId} заменена на {x.CategoryId} (запрещено менять категорию)",
                                false))
                        .ToList();

                    List<ValidationResultItem> changedNames = GetChangedNames(result).ToList();

                    if (changedCategoryWarnings.Any() || changedNames.Any())
                    {
                        ShowValidationResultView("Предупреждения при импорте", changedNames.Concat(changedCategoryWarnings).ToList());
                    }
                }
                else
                {
                    ShowValidationResultView("Ошибки при импорте", result.Errors);
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {fileName} занят другим процессом");
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при импорте", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", fileName);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }

            IEnumerable<ValidationResultItem> GetChangedNames(ExcelImportResult<ProductCatalogImportItem> result)
            {
                if (IsAllowEditProductNameAnyTime)
                {
                    return Array.Empty<ValidationResultItem>();
                }
                else
                {
                    return result.ResultItems
                    .Where(x => x.Dto != null &&
                                ProductHelper.IsProductEditingOvertimed(x.Dto.CreatedOn) &&
                                x.Name?.Equals(x.Dto.Name) == false)
                    .Select(
                        x => new ValidationResultItem(
                            $"Название '{x.Dto.Name}' заменено на '{x.Name}' (Товар старше {ProductHelper.ProductEditingOvertimeDays} дней)",
                            false));
                }
            }
        }

        private void SetFilterType(ProductsCatalogFilterType type)
        {
            ProductsCatalogFilterType oldFilterType = FilterType;
            FilterType = type;

            if (oldFilterType == type)
            {
                RaisePropertyChanged(nameof(FilterType));
            }
        }

        private void GetCategory()
        {
            if (Filter.SelectedCategory == null)
            {
                return;
            }

            GetProductCategoryParameter parameter = new GetProductCategoryParameter(loadedCategory.Id, SelectedProduct.CategoryId);

            GetProductCategoryViewModel result = DialogDocumentManagerService.ShowView<GetProductCategoryViewModel>(parameter, this);

            if (!result.IsOk || result.SelectedCategory == null)
            {
                return;
            }

            if (result.SelectedCategory.ParentLevel < 0)
            {
                MessageFacadeService.ShowNotificationWarning("Запрещено добавлять товары выше родительской категории");
                return;
            }

            SelectedProduct.SetCategory(result.SelectedCategory);
            SelectedProduct.SetSupportedFeatures(categoryFeatures.AsReadOnly()
                .GetValueOrDefault(result.SelectedCategory.Id));

            RaisePropertyChanged(nameof(Features));
        }
    }
}