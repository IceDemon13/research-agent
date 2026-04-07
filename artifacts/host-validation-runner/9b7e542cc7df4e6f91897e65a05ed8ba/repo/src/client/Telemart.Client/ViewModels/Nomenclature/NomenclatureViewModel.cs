using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Utils;
using DevExpress.Xpf.Data;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.CategoryFilters;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Controls.Accordion;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using CategoryDto = Telemart.Client.TransferObjects.CategoryDto;

namespace Telemart.Client.ViewModels.Nomenclature
{
    public sealed class NomenclatureViewModel : TelemartDialogViewModelBase
    {
        private const int PageSize = 100;

        private NomenclatureViewOptions _options;
        private int? _searchCategoryId;
        private string _searchText;

        public NomenclatureViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            SearchCommand = new DelegateCommand<int?>(SearchByPattern);
            SearchByFilterCommand = new AsyncCommand(SearchFilterAsync);
            HandleRowDoubleClickCommand = new DelegateCommand(HandleRowDoubleClick);
            RemoveCartItemCommand = new DelegateCommand<NomenclatureViewItem>(RemoveCartItem, x => x != null);
            ApplyFilterCommand = new DelegateCommand(ApplyFilter);
            CancelFilterCommand = new DelegateCommand(CancelFilter);

            ProductInformation = productInformationViewModel;

            FiltersLoader = new CategoryFiltersLoader(WebClient);

            Products = new PagedAsyncSource();

            Products.PageSize = PageSize;
            Products.ElementType = typeof(NomenclatureViewItem);
            Products.PageNavigationMode = PageNavigationMode.ArbitraryWithTotalPageCount;
            Products.GetTotalSummaries += ProductsOnGetTotalSummaries;
            Products.FetchPage += PagingHelper.FetchEmptyItems;
        }

        public NomenclatureViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand SearchCommand { get; }

        public IAsyncCommand SearchByFilterCommand { get; }

        public IDelegateCommand RemoveCartItemCommand { get; }

        public IDelegateCommand ApplyFilterCommand { get; }

        public IDelegateCommand CancelFilterCommand { get; }

        #endregion

        public int SelectedTabIndex
        {
            get { return GetProperty(() => SelectedTabIndex); }
            set { SetProperty(() => SelectedTabIndex, value); }
        }

        #region Search

        public ObservableCollection<CategoryViewItem> SearchCategories
        {
            get { return GetProperty(() => SearchCategories); }
            private set { SetProperty(() => SearchCategories, value); }
        }

        public CategoryViewItem CurrentSearchCategory
        {
            get { return GetProperty(() => CurrentSearchCategory); }
            set { SetProperty(() => CurrentSearchCategory, value); }
        }

        public string SearchText
        {
            get { return GetProperty(() => SearchText); }
            set { SetProperty(() => SearchText, value); }
        }

        #endregion

        #region Filter
        public ReadOnlyObservableCollection<ComboBoxItem> FilterLanguages
        {
            get { return GetProperty(() => FilterLanguages); }
            private set { SetProperty(() => FilterLanguages, value); }
        }

        public ComboBoxItem SelectedFilterLanguage
        {
            get { return GetProperty(() => SelectedFilterLanguage); }
            set { SetProperty(() => SelectedFilterLanguage, value); }
        }

        public ObservableCollection<CategoryViewItem> FilterCategories
        {
            get { return GetProperty(() => FilterCategories); }
            private set { SetProperty(() => FilterCategories, value); }
        }

        public CategoryViewItem SelectedFilterCategory
        {
            get { return GetProperty(() => SelectedFilterCategory); }
            set { SetProperty(() => SelectedFilterCategory, value); }
        }

        public ObservableCollection<RootAccordionItem> FilterItems
        {
            get { return GetProperty(() => FilterItems); }
            private set { SetProperty(() => FilterItems, value); }
        }

        public ReadOnlyObservableCollection<ProductAvailability> Availabilities
        {
            get { return GetProperty(() => Availabilities); }
            private set { SetProperty(() => Availabilities, value); }
        }

        public ObservableCollection<int> SelectedAvailabilities
        {
            get { return GetProperty(() => SelectedAvailabilities); }
            set { SetProperty(() => SelectedAvailabilities, value); }
        }

        public bool ShowCompatibility
        {
            get { return GetProperty(() => ShowCompatibility); }
            set { SetProperty(() => ShowCompatibility, value); }
        }

        public bool ShowCompatibilityWithoutWarnings
        {
            get { return GetProperty(() => ShowCompatibilityWithoutWarnings); }
            set { SetProperty(() => ShowCompatibilityWithoutWarnings, value); }
        }

        public bool CompatibleOnly
        {
            get { return GetProperty(() => CompatibleOnly); }
            set { SetProperty(() => CompatibleOnly, value); }
        }

        public bool CompatibleWithoutWarningsOnly
        {
            get { return GetProperty(() => CompatibleWithoutWarningsOnly); }
            set { SetProperty(() => CompatibleWithoutWarningsOnly, value); }
        }

        #endregion

        #region Common

        public PagedAsyncSource Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public NomenclatureViewItem CurrentProduct
        {
            get { return GetProperty(() => CurrentProduct); }
            set { SetProperty(() => CurrentProduct, value, CurrentProductChangedCallBack); }
        }

        public ObservableRangeCollection<NomenclatureViewItem> CartItems { get; } = new ObservableRangeCollection<NomenclatureViewItem>();

        public NomenclatureViewItem CurrentCartItem
        {
            get { return GetProperty(() => CurrentCartItem); }
            set { SetProperty(() => CurrentCartItem, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        #endregion

        #region ProductOptions

        public bool ShowPrice
        {
            get { return GetProperty(() => ShowPrice); }
            private set { SetProperty(() => ShowPrice, value); }
        }

        public DefaultBoolean CanEditPrice
        {
            get { return GetProperty(() => CanEditPrice); }
            private set { SetProperty(() => CanEditPrice, value); }
        }

        public DefaultBoolean CanEditQuantity
        {
            get { return GetProperty(() => CanEditQuantity); }
            private set { SetProperty(() => CanEditQuantity, value); }
        }

        #endregion

        public bool ShowButtons
        {
            get { return GetProperty(() => ShowButtons); }
            private set { SetProperty(() => ShowButtons, value); }
        }

        public bool ShowCart
        {
            get { return GetProperty(() => ShowCart); }
            private set { SetProperty(() => ShowCart, value); }
        }

        public long? TotalCount
        {
            get { return GetProperty(() => TotalCount); }
            private set { SetProperty(() => TotalCount, value); }
        }

        #region DialogSettings

        public override int Height => 576;

        public override int MinHeight => 576;

        public override int MinWidth => 1024;

        public override int Width => 1024;

        #endregion

        private IMapper Mapper { get; }

        private CategoryFiltersLoader FiltersLoader { get; }

        public IEnumerable<NomenclatureViewItem> GetSelectedItems()
        {
            return CartItems;
        }

        protected override async Task HandleLoadedAsync()
        {
            const int FilterTabIndex = 1;

            Title = "Выбор товара";

            PagedResult<CategoryDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            FilterCategories = pagedResult.Data
                .OrderBy(x => x.Position)
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToObservableCollection();
            FilterLanguages = Dictionaries
                .GetItems<Language>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            SelectedFilterLanguage = FilterLanguages.First(x => x.Id == Language.UkrainianId);

            if (_options.SelectedCategoryId.HasValue)
            {
                SelectedFilterCategory = FilterCategories.FirstOrDefault(x => x.Id == _options.SelectedCategoryId);

                SelectedTabIndex = FilterTabIndex;
            }

            Availabilities = Dictionaries.GetItems<ProductAvailability>()
                .Where(x => x.Active)
                .OrderByDescending(x => x.Weight)
                .ToReadOnlyObservableCollection();

            ResetAvailabilities();
        }

        protected override void HandleCancel()
        {
            if (_options.FullScreenMode)
            {
                return;
            }

            Close();
        }

        protected override Task HandleOkAsync()
        {
            if (_options.FullScreenMode)
            {
                return Task.CompletedTask;
            }

            if (GetSelectedItems().Any())
            {
                IsOk = true;
                Close();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Вы не выбрали ни один товар");
            }

            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                return;
            }

            _options = (NomenclatureViewOptions)parameter;

            ShowPrice = _options.ShowPrice;
            CanEditPrice = _options.CanEditPrice
                ? DefaultBoolean.True
                : DefaultBoolean.False;
            CanEditQuantity = _options.SelectionMode == NomenclatureViewSelectionMode.ByQuantity
                ? DefaultBoolean.True
                : DefaultBoolean.False;
            SearchText = _options.SearchText;
            ShowButtons = !_options.FullScreenMode;
            ShowCart = !_options.FullScreenMode;

            if (_options.CompatibleWithProducts?.Any() == true)
            {
                ShowCompatibility = true;
                ShowCompatibilityWithoutWarnings = true;
                CompatibleOnly = true;
                CompatibleWithoutWarningsOnly = true;
            }
            else
            {
                ShowCompatibility = false;
                ShowCompatibilityWithoutWarnings = false;
                CompatibleOnly = false;
                CompatibleWithoutWarningsOnly = false;
            }
        }

        private static CategoryViewItem MapCategory(CatalogCategoryDto catalogCategory)
        {
            CategoryViewItem category = CategoryViewItem.Create();

            category.Id = catalogCategory.Id;
            category.ParentId = catalogCategory.ParentId;
            category.Name = $"{catalogCategory.Name} ({catalogCategory.FoundQuantity})";
            category.Level = catalogCategory.Level;
            category.ParentLevel = catalogCategory.ParentLevel;
            category.IsParent = catalogCategory.IsParent == 1;
            category.Active = catalogCategory.Active;

            return category;
        }

        private void ResetAvailabilities()
        {
            SelectedAvailabilities = Availabilities
                .Where(x => x.Type == ProductAvailabilityType.InStock)
                .Select(x => x.Id)
                .ToObservableCollection();
        }

        private void HandleRowDoubleClick()
        {
            NomenclatureViewItem cartItem = CartItems.FirstOrDefault(x => x.Id == CurrentProduct.Id);

            switch (_options.SelectionMode)
            {
                case NomenclatureViewSelectionMode.ByQuantity:
                case NomenclatureViewSelectionMode.ByCheck:
                    {
                        if (cartItem == null)
                        {
                            cartItem = CurrentProduct.Clone();
                            cartItem.Quantity = 1;

                            CartItems.Add(cartItem);
                        }
                        else
                        {
                            int quantity = _options.SelectionMode == NomenclatureViewSelectionMode.ByQuantity
                                ? 1
                                : 0;

                            cartItem.Quantity += quantity;
                        }

                        break;
                    }

                case NomenclatureViewSelectionMode.Single:
                    {
                        if (cartItem == null)
                        {
                            cartItem = CurrentProduct.Clone();
                            cartItem.Quantity = 1;
                            CartItems.Add(cartItem);

                            OkCommand.Execute(null);
                        }

                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private void SearchByPattern(int? categoryId)
        {
            if (Products.AreRowsFetching)
            {
                return;
            }

            _searchCategoryId = categoryId;

            ClearFetchPageEvent();
            Products.FetchPage += ProductsFetchPageByPattern;

            Products.PageIndex = 0;
            Products.RefreshRows();
        }

        private async Task<FetchRowsResult> SearchByPatternInternalAsync(int skip, int take, SortDefinition sortDefinition)
        {
            FetchRowsResult fetchRowsResult = null;

            try
            {
                if (!string.Equals(SearchText, _searchText, StringComparison.Ordinal))
                {
                    SearchCategories?.Clear();
                }

                _searchText = SearchText;

                ProductSort sortText = SortRule(sortDefinition);

                QueryProductByPattern request = new QueryProductByPattern(
                    _options.ContractorId,
                    SearchText,
                    sortText,
                    _searchCategoryId,
                    _options.QueryGifts,
                    _options.ExcludeDiscounts,
                    _options.QueryAdditionalServices,
                    _options.QueryPriceIn,
                    _options.PriceInIncludeReserve,
                    _options.IncludePriceJson,
                    skip,
                    take,
                    sortText == ProductSort.None,
                    _options.AdditionalServiceProvideProductsByProductId,
                    _options.CartProductIds,
                    SelectedFilterLanguage.Id);

                ProductsPagedResult pagedResult = await WebClient.ExecuteCatalogApiRequestAsync(request);

                TotalCount = pagedResult.Pagination.TotalCount;
                Products.UpdateSummaries();

                if (pagedResult.Pagination.Returned > 0)
                {
                    ProcessCategories(pagedResult.Categories);
                }
                else
                {
                    SearchCategories?.Clear();
                    MessageFacadeService.ShowNotificationWarning($"По запросу \"{SearchText}\" ничего не найдено");
                }

                object[] rows = await MapProductsAsync(pagedResult.Data, sortDefinition);

                fetchRowsResult = new FetchRowsResult(rows, pagedResult.Pagination.HasMoreRows);
            }
            catch (Exception exception)
            {
                SearchCategories?.Clear();
                Logger.LogError(exception, "Failed to load products");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            RaisePropertyChanged(nameof(CurrentSearchCategory));

            return fetchRowsResult;
        }

        private void ProductsFetchPageByPattern(object sender, FetchPageAsyncEventArgs e)
        {
            SortDefinition sortRule = e.SortOrder.SingleOrDefault();

            e.Result = SearchByPatternInternalAsync(e.Skip, e.Take, sortRule);
        }

        private async Task SearchFilterAsync()
        {
            FilterItems = null;

            if (SelectedFilterCategory == null)
            {
                return;
            }

            try
            {
                _searchText = string.Empty;
                SearchText = string.Empty;
                SearchCategories?.Clear();

                IEnumerable<RootAccordionItem> filterItems = await FiltersLoader.QueryCategoryFiltersAsync(
                    SelectedFilterCategory.Id,
                    _options.ContractorId);

                FilterItems = filterItems.ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load filters or products");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task<object[]> MapProductsAsync(IReadOnlyCollection<ProductDto> products, SortDefinition sortDefinition)
        {
            if (products.Any() && _options.PriceContext == NomenclatureViewPriceContext.Supplier)
            {
                await ProcessProductPricesAsync(products);

                if (sortDefinition != null)
                {
                    switch (sortDefinition.PropertyName, sortDefinition.Direction)
                    {
                        case (nameof(NomenclatureViewItem.Price), ListSortDirection.Ascending):
                            return products.OrderBy(x => x.Price).Select(x => Mapper.Map<NomenclatureViewItem>(x)).Cast<object>().ToArray();
                        case (nameof(NomenclatureViewItem.Price), ListSortDirection.Descending):
                            return products.OrderByDescending(x => x.Price).Select(x => Mapper.Map<NomenclatureViewItem>(x)).Cast<object>().ToArray();
                    }
                }
            }

            return products.Select(x => Mapper.Map<NomenclatureViewItem>(x)).Cast<object>().ToArray();
        }

        private async Task ProcessProductPricesAsync(IReadOnlyCollection<ProductDto> products)
        {
            QueryContractorPrices request = new QueryContractorPrices(
                _options.ContractorId,
                products.Select(x => x.Id).ToArray());

            List<ParserContractorPriceDto> contractorPriceDtos = await WebClient.ExecuteApiRequestAsync(request);

            Dictionary<int, ParserContractorPriceDto> contractorPrices = contractorPriceDtos.ToDictionary(x => x.ProductId);

            foreach (ProductDto product in products)
            {
                if (contractorPrices.TryGetValue(product.Id, out ParserContractorPriceDto contractorPrice))
                {
                    product.Price = contractorPrice.Price;
                    product.CurrencyId = contractorPrice.CurrencyId;
                }
                else
                {
                    product.Price = 0;
                    product.CurrencyId = product.CurrencyId;
                }
            }
        }

        private void ApplyFilter()
        {
            if (Products.AreRowsFetching)
            {
                return;
            }

            ClearFetchPageEvent();

            Products.FetchPage += ProductsFetchPageByFilter;

            Products.PageIndex = 0;
            Products.RefreshRows();
        }

        private void CancelFilter()
        {
            if (Products.AreRowsFetching || SelectedFilterCategory == null)
            {
                return;
            }

            FilterItems.ForEach(x => x.Cancel());

            ResetAvailabilities();

            ClearFetchPageEvent();

            Products.FetchPage += ProductsFetchPageByFilter;

            Products.PageIndex = 0;
            Products.RefreshRows();
        }

        private void ProductsFetchPageByFilter(object sender, FetchPageAsyncEventArgs e)
        {
            SortDefinition sortRule = e.SortOrder.SingleOrDefault();
            e.Result = SearchByFilterAsync(e.Skip, e.Take, sortRule);
        }

        private async Task<FetchRowsResult> SearchByFilterAsync(int skip, int take, SortDefinition sortDefinition)
        {
            if (SelectedFilterCategory == null || FilterItems == null)
            {
                return null;
            }

            List<int> filterIds = new List<int>();
            List<int> labelIds = new List<int>();
            List<int> warehouseIds = new List<int>();
            decimal? minPrice = null;
            decimal? maxPrice = null;

            foreach (RootAccordionItem rootAccordionItem in FilterItems)
            {
                switch (rootAccordionItem.Item)
                {
                    case RangeAccordionItem range when string.Equals(range.Name, CategoryFiltersLoader.PriceItemName, StringComparison.Ordinal):
                        minPrice = range.SelectionMinimum;
                        maxPrice = range.SelectionMaximum;
                        break;
                    case CheckedListAccordionItem list when list.SelectedItems?.Any() == true:
                        switch (list.Name)
                        {
                            case CategoryFiltersLoader.LabelItemName:
                                labelIds.AddRange(list.SelectedItems);
                                break;
                            case CategoryFiltersLoader.WarehouseItemName:
                                warehouseIds.AddRange(list.SelectedItems);
                                break;
                            default:
                                filterIds.AddRange(list.SelectedItems);
                                break;
                        }

                        break;
                }
            }

            IReadOnlyCollection<ProductQuantityDto> compatibleWithProducts = CompatibleOnly
                ? _options.CompatibleWithProducts?.Select(x => new ProductQuantityDto(x.ProductId, x.Quantity)).ToArray()
                : null;

            ProductSort productSort = SortRule(sortDefinition);

            QueryProductByFilter request = new QueryProductByFilter(
                _options.ContractorId,
                SelectedFilterCategory.Id,
                filterIds,
                labelIds,
                warehouseIds,
                SelectedAvailabilities,
                compatibleWithProducts,
                CompatibleWithoutWarningsOnly,
                minPrice,
                maxPrice,
                _options.QueryGifts,
                productSort,
                true,
                _options.QueryAdditionalServices,
                _options.QueryPriceIn,
                _options.PriceInIncludeReserve,
                skip,
                take,
                productSort == ProductSort.None,
                searchAdditionalServiceProvideProductsByProductId: _options.AdditionalServiceProvideProductsByProductId,
                cartProductIds: _options.CartProductIds,
                productTypeIds: _options.ProductTypeIds);

            PagedResult<ProductDto> pagedResult = await WebClient.ExecuteCatalogApiRequestAsync(request);

            TotalCount = pagedResult.Pagination.TotalCount;
            Products.UpdateSummaries();

            object[] rows = await MapProductsAsync(pagedResult.Data, sortDefinition);

            return new FetchRowsResult(rows, pagedResult.Pagination.HasMoreRows);
        }

        private void CurrentProductChangedCallBack()
        {
            ProductInformation.ClearProduct();

            if (CurrentProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(CurrentProduct.Id, CurrentProduct.CurrencyId);
            }
        }

        private void RemoveCartItem(NomenclatureViewItem cartItem)
        {
            CartItems.Remove(cartItem);
        }

        private void ClearFetchPageEvent()
        {
            Products.FetchPage -= ProductsFetchPageByPattern;
            Products.FetchPage -= ProductsFetchPageByFilter;
            Products.FetchPage -= PagingHelper.FetchEmptyItems;
        }

        private void ProductsOnGetTotalSummaries(object sender, GetSummariesAsyncEventArgs e)
        {
            e.Result = Task.FromResult(e.Summaries
                .Select(x => x.SummaryType == SummaryType.Count ? TotalCount : null)
                .Cast<object>()
                .ToArray());
        }

        private ProductSort SortRule(SortDefinition sortDefinition)
        {
            if (sortDefinition == null)
            {
                return ProductSort.None;
            }

            switch (sortDefinition.PropertyName, sortDefinition.Direction)
            {
                case (nameof(NomenclatureViewItem.Id), ListSortDirection.Ascending):
                    return ProductSort.IdAsc;
                case (nameof(NomenclatureViewItem.Id), ListSortDirection.Descending):
                    return ProductSort.IdDesc;
                case (nameof(NomenclatureViewItem.NameFullRu), ListSortDirection.Ascending):
                    return ProductSort.NameFullAsc;
                case (nameof(NomenclatureViewItem.NameFullRu), ListSortDirection.Descending):
                    return ProductSort.NameFullDesc;
                case (nameof(NomenclatureViewItem.WarehouseQuantity), ListSortDirection.Ascending):
                    return ProductSort.WarehouseQuantityFreeAsc;
                case (nameof(NomenclatureViewItem.WarehouseQuantity), ListSortDirection.Descending):
                    return ProductSort.WarehouseQuantityFreeDesc;
                case (nameof(NomenclatureViewItem.Price), ListSortDirection.Ascending):
                    return _options.PriceContext == NomenclatureViewPriceContext.Supplier ? ProductSort.None : ProductSort.Price1Asc;
                case (nameof(NomenclatureViewItem.Price), ListSortDirection.Descending):
                    return _options.PriceContext == NomenclatureViewPriceContext.Supplier ? ProductSort.None : ProductSort.Price1Desc;

                default:
                    return ProductSort.None;
            }
        }

        private void ProcessCategories(IReadOnlyCollection<CatalogCategoryDto> categories)
        {
            SearchCategories = categories.Where(x => x.ParentLevel <= 0).Select(MapCategory).ToObservableCollection();
            CurrentSearchCategory = null;

            if (_searchCategoryId.HasValue)
            {
                CurrentSearchCategory = SearchCategories.FirstOrDefault(x => x.Id == _searchCategoryId.Value);
            }
        }
    }
}