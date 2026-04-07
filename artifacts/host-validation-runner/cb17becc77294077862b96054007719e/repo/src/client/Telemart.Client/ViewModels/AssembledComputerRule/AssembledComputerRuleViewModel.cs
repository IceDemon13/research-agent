using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Newtonsoft.Json.Linq;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Products.Colors;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Directories.ProductsCatalog;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRuleViewModel : TelemartEditorViewModelBase<AssembledComputerRuleDto, AssembledComputerRuleParameter, AssembledComputerRuleViewItem>
    {
        public AssembledComputerRuleViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands,
            IErrorHandler errorHandler,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddCategoryCommand = new DelegateCommand(AddCategory);
            AddProductCommand = new AsyncCommand(AddProductAsync, () => SelectedCategory != null);
            DeleteCategoryCommand = new DelegateCommand(DeleteCategory, () => SelectedCategory != null);
            IgnoreRecalculationRulesCommand = new DelegateCommand(IgnoreRecalculationRules);
            DeleteProductCommand = new DelegateCommand(DeleteProduct, () => SelectedProduct != null);
            ReorderProductPriorityCommand = new DelegateCommand(ReorderProductPriority, () => Products?.Count > 1);
            OnCategoriesCellValueChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(OnCategoriesCellValueChanged);
            SelectBaseProductCommand = new AsyncCommand(SelectBaseProductAsync);
            ClearBaseProductCommand = new DelegateCommand(ClearBaseProduct);
            AssembledComputerRuleCopyCommand = new AsyncCommand(AssembledComputerRuleCopyAsync);
            CheckCommand = new AsyncCommand(CheckAsync);

            DocumentCommands = documentCommands;
            ErrorHandler = errorHandler;
            ProductInformation = productInformationViewModel;
        }

        public AssembledComputerRuleViewModel()
        {
        }

        public IDelegateCommand AddCategoryCommand { get; }

        public IAsyncCommand AddProductCommand { get; }

        public IDelegateCommand DeleteCategoryCommand { get; }

        public IDelegateCommand IgnoreRecalculationRulesCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IAsyncCommand SelectBaseProductCommand { get; }

        public IDelegateCommand ClearBaseProductCommand { get; }

        public IDelegateCommand ReorderProductPriorityCommand { get; }

        public IDelegateCommand OnCategoriesCellValueChangedCommand { get; }

        public IAsyncCommand AssembledComputerRuleCopyCommand { get; }

        public IAsyncCommand CheckCommand { get; }

        public AssembledComputerRuleCategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, RefreshProducts); }
        }

        public AssembledComputerRuleProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value, RefreshProductInfo); }
        }

        public IReadOnlyDictionary<int, ProductColorViewItem> ProductColorsDictionary
        {
            get { return GetProperty(() => ProductColorsDictionary); }
            set { SetProperty(() => ProductColorsDictionary, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<BrandViewItem> Brands
        {
            get { return GetProperty(() => Brands); }
            private set { SetProperty(() => Brands, value); }
        }

        public ObservableCollection<ProductColorViewItem> ProductColors
        {
            get { return GetProperty(() => ProductColors); }
            private set { SetProperty(() => ProductColors, value); }
        }

        public ReadOnlyObservableCollection<AssembledComputerRuleProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ReadOnlyObservableCollection<ProductType> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public override int MinHeight => 555;

        public override int Height => 625;

        public override int MaxHeight => 1080;

        public override int MinWidth => 1000;

        public override int Width => 1000;

        public override int MaxWidth => 1920;

        protected override string CreatedActionMessage => "создано";

        protected override string EntityName => "Правило";

        protected override string UpdatedActionMessage => "соxранено";

        private DocumentCommands DocumentCommands { get; }

        private IErrorHandler ErrorHandler { get; }

        public override void OnDestroy()
        {
            Model.Categories?.ForEach(x => x.ProductTypesChanged -= CategoryProductTypesChanged);

            base.OnDestroy();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила конфигурации готового ПК";
        }

        protected override void SetEditTitle()
        {
            Title = $"Правило конфигурации готового ПК ({Model.Id})";
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
               .Select(x => new ComboBoxItem(x.Id, x.Name))
               .ToReadOnlyObservableCollection();

            Brands = categories
               .Where(x => x.ParentId is Constants.AssembledComputersCategoryId or Constants.AssembledComputersMinerCategoryId)
               .Select(x => new BrandViewItem(x.Id, $"{x.ParentName}/{x.Name}", x.Name, x.PrefixRus, x.PrefixUkr, x.PrefixEn))
               .ToReadOnlyObservableCollection();

            List<ProductColorDto> colors = await WebClient.ExecuteApiRequestAsync(new QueryProductColors(), true)
               .GetPagedResultDataAsync();

            ProductColors = Mapper.Map<List<ProductColorViewItem>>(colors)
                .OrderBy(x => x.Name)
                .ToObservableCollection();

            ProductColorsDictionary = ProductColors.ToDictionary(x => x.Id);

            ProductTypes = Dictionaries.GetItems<ProductType>()
                .Where(x => x.AllowedInAssembledComputerRule)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Model.Categories?.ForEach(x => x.ProductTypesChanged += CategoryProductTypesChanged);
        }

        protected override Task<Result<AssembledComputerRuleDto>> CreateEntityAsync()
        {
            AssembledComputerRuleCreateDto createDto = new AssembledComputerRuleCreateDto()
            {
                Name = Model.Name,
                Color = Model.Color ?? string.Empty,
                Model = Model.Model,
                Modific = Model.Modific,
                Manufactor = Model.Brand != null ? Model.Brand.Value.Name : string.Empty,
                Prefix = Model.Prefix,
                PrefixUkr = Model.PrefixUkr,
                PrefixEn = Model.PrefixEn,
                PartNumber = Model.PartNumber,
                ColorPrimaryId = Model.ColorPrimaryId,
                ColorSecondaryId = Model.ColorSecondaryId,
                CategoryId = Model.Brand?.Id ?? 0,
                Active = Model.Active,
                BaseProductId = Model.BaseProductId,
                IgnoreSlotConsumerIds = Model.IgnoreSlotConsumerIds?.ToArray(),

                Categories = Model.Categories?.Select(x =>
                    new AssembledComputerRuleCategoryCreateDto()
                    {
                        CategoryId = x.CategoryId,
                        FeatureId = x.FeatureId,
                        FeatureValueId = x.FeatureValueId,
                        Quantity = x.Quantity,
                        UseAnyProducts = x.UseAnyProducts,
                        CompareMethod = x.CompareMethod,
                        ProductTypeIds = x.ProductTypes.Select(y => y.Id).ToArray()
                    }).ToList(),
                Products = Model.Products?.Select(z =>
                    new AssembledComputerRuleProductCreateDto()
                    {
                        Priority = z.Priority,
                        ProductId = z.ProductId,
                        Required = z.Required,
                        QuantityToUse = z.QuantityToUse
                    }).ToList()
            };

            CreateAssembledComputerRule gatewayRequest = new CreateAssembledComputerRule(createDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<AssembledComputerRuleDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRule(id));
        }

        protected override Task<LockResponse<AssembledComputerRuleDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<AssembledComputerRuleDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void AfterSetData()
        {
            Model.ColorPrimary = ProductColorsDictionary.GetValueOrDefault(Model.ColorPrimaryId ?? -1, null);
            Model.ColorSecondary = ProductColorsDictionary.GetValueOrDefault(Model.ColorSecondaryId ?? -1, null);

            ModelOriginal.ColorPrimary = Model.ColorPrimary;
            ModelOriginal.ColorSecondary = Model.ColorSecondary;

            SelectedCategory = Model.Categories?.FirstOrDefault();

            if (!string.IsNullOrEmpty(EditorParameter.ProductName))
            {
                var firstMatchedProduct = Model.Products
                    .OrderBy(x => x.Priority)
                    .FirstOrDefault(x => x.ProductName.Contains(EditorParameter.ProductName));

                if (firstMatchedProduct != null)
                {
                    SelectedCategory = Model.Categories?.FirstOrDefault(x => x.CategoryId == firstMatchedProduct?.ParentCategoryId);
                }
            }

            RefreshProducts();
        }

        protected override Task<Result<AssembledComputerRuleDto>> UpdateEntityAsync()
        {
            AssembledComputerRuleUpdateDto saveDto = new()
            {
                Name = Model.Name,
                Color = Model.Color ?? string.Empty,
                Model = Model.Model,
                Modific = Model.Modific,
                Prefix = Model.Prefix,
                PrefixUkr = Model.PrefixUkr,
                PrefixEn = Model.PrefixEn,
                PartNumber = Model.PartNumber,
                Manufactor = Model.Brand.Value.Name,
                ColorPrimaryId = Model.ColorPrimaryId,
                ColorSecondaryId = Model.ColorSecondaryId,
                Active = Model.Active,
                BaseProductId = Model.BaseProductId,
                IgnoreSlotConsumerIds = Model.IgnoreSlotConsumerIds?.ToArray(),

                Categories = Model.Categories.Select(x =>
                new AssembledComputerRuleCategoryUpdateDto()
                {
                    CategoryId = x.CategoryId,
                    FeatureId = x.FeatureId,
                    FeatureValueId = x.FeatureValueId,
                    Quantity = x.Quantity,
                    Id = x.Id,
                    UseAnyProducts = x.UseAnyProducts,
                    CompareMethod = x.CompareMethod,
                    ProductTypeIds = x.ProductTypes.Select(y => y.Id).ToArray()
                }).ToList(),

                Products = Model.Products.Select(z =>
                    new AssembledComputerRuleProductUpdateDto()
                    {
                        Id = z.Id,
                        Priority = z.Priority,
                        ProductId = z.ProductId,
                        Required = z.Required,
                        QuantityToUse = z.QuantityToUse
                    }).ToList()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateAssembledComputerRule(Model.Id, saveDto));
        }

        private void RefreshProductInfo()
        {
            ProductInformation.ClearProduct();

            if (SelectedProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(SelectedProduct.ProductId, Currency.UahId);
            }
        }

        private void AddCategory()
        {
            AssembledComputerRuleCreateCategoryParameter parameter = new AssembledComputerRuleCreateCategoryParameter(
                Model.Categories
                    .GroupBy(x => x.CategoryId)
                    .ToDictionary(x => x.Key, y => y.SelectMany(z => z.ProductTypes).Select(z => z.Id).Distinct().ToArray()));

            AssembledComputerRuleCreateCategoryViewModel viewModel
                = DialogDocumentManagerService.ShowView<AssembledComputerRuleCreateCategoryViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.Categories?.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id && viewModel.SelectedFeature.HasValue && x.FeatureId == viewModel.SelectedFeature.Value.Id && viewModel.SelectedFeatureValue.HasValue && x.FeatureValueId == viewModel.SelectedFeatureValue.Value.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория с такой характеристикой уже добавлена");
                return;
            }

            string categoryName = Categories.FirstOrDefault(x => x.Id == viewModel.SelectedCategory.Id).DisplayValue;

            bool useAnyProducts = Model.Categories?.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id)?.UseAnyProducts ?? true;

            var productTypes = viewModel.ProductTypes.Where(x => viewModel.SelectedProductTypeIds.Contains(x.Id)).ToObservableCollection();
            AssembledComputerRuleCategoryViewItem category = new AssembledComputerRuleCategoryViewItem
            {
                CategoryId = viewModel.SelectedCategory.Id,
                CategoryName = categoryName,
                FeatureName = viewModel.SelectedFeature.HasValue ? viewModel.SelectedFeature.Value.DisplayValue : null,
                FeatureId = viewModel.SelectedFeature.HasValue ? viewModel.SelectedFeature.Value.Id : null,
                FeatureValueName = viewModel.SelectedFeatureValue.HasValue ? viewModel.SelectedFeatureValue.Value.DisplayValue : null,
                FeatureValueId = viewModel.SelectedFeatureValue.HasValue ? viewModel.SelectedFeatureValue.Value.Id : null,
                Quantity = 1,
                UseAnyProducts = useAnyProducts,
                CompareMethod = viewModel.SelectedOperation,
                ProductTypes = productTypes
            };

            category.ProductTypesChanged += CategoryProductTypesChanged;

            Model.Categories.Add(category);

            Model.Categories.Where(x => x.CategoryId == viewModel.SelectedCategory.Id).ForEach(x => x.ProductTypes = productTypes);

            MessageFacadeService.ShowNotificationInfo("Категория успешно добавлена");
        }

        private void DeleteCategory()
        {
            if (!Model.Categories.Any(x => x.CategoryId == SelectedCategory.CategoryId && x.Id != SelectedCategory.Id)
                && Model.Products.Any(x => x.ParentCategoryId == SelectedCategory.CategoryId))
            {
                if (!MessageFacadeService.Confirm("Также будут удалены товары принадлежащие категории. Вы уверены?"))
                {
                    return;
                }
            }

            SelectedCategory.ProductTypesChanged -= CategoryProductTypesChanged;

            Model.Categories.Remove(SelectedCategory);

            MessageFacadeService.ShowNotificationInfo("Категория успешно удалена");
        }

        private void IgnoreRecalculationRules()
        {
            AssembledComputerRuleIgnoreParameter parameter = new AssembledComputerRuleIgnoreParameter(Model.IgnoreSlotConsumerIds);

            AssembledComputerRuleIgnoreViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<AssembledComputerRuleIgnoreViewModel>(parameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Model.IgnoreSlotConsumerIds = viewModel.GetIgnoreSlotConsumerIds().ToObservableCollection();
        }

        private void DeleteProduct()
        {
            Model.Products.Remove(SelectedProduct);

            if (Model.Products.All(x => x.ParentCategoryId != SelectedProduct.ParentCategoryId))
            {
                IEnumerable<AssembledComputerRuleCategoryViewItem> categories = Model.Categories.Where(x => x.CategoryId == SelectedProduct.ParentCategoryId);

                foreach (AssembledComputerRuleCategoryViewItem category in categories)
                {
                    if (category != null)
                    {
                        category.UseAnyProducts = true;
                    }
                }
            }

            RefreshProducts();

            MessageFacadeService.ShowNotificationInfo("Товар успешно удален");
        }

        private async Task AddProductAsync()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByQuantity,
                false,
                selectedCategoryId: SelectedCategory.CategoryId,
                productTypeIds: SelectedCategory.ProductTypes?.Any() == true ? SelectedCategory.ProductTypes.Select(x => x.Id).ToArray() : null);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                List<ProductFeatureGroupsDto> productsFeatureGroups = await WebClient.ExecuteApiRequestAsync(new QueryProductFeatures(nomenclatureViewModel.GetSelectedItems().Select(x => x.Id).ToArray()));

                List<string> errors = new List<string>();

                foreach (NomenclatureViewItem product in nomenclatureViewModel.GetSelectedItems())
                {
                    if (Model.Products.Select(x => x.ProductId).Contains(product.Id))
                    {
                        errors.Add($"Товар '{product.Name}' ({product.Id}) уже добавлен в выбранную категорию");
                        continue;
                    }

                    if (SelectedCategory.CategoryId != product.ParentCategoryId)
                    {
                        errors.Add($"Товар '{product.Name}' ({product.Id}) не относится к выбранной категории");
                        continue;
                    }

                    if (!SelectedCategory.ProductTypes.Select(x => x.Id).Contains(product.TypeId))
                    {
                        errors.Add($"Товар '{product.Name}' ({product.Id}) не соответствует выбранным в категории типам товаров");
                        continue;
                    }

                    Dictionary<int, int[]> productFeatures = productsFeatureGroups
                        .Where(x => x.ProductId == product.Id)
                        .SelectMany(x => x.AttributeGroups)
                        .SelectMany(x => x.Attributes)
                        .GroupBy(x => x.Id)
                        .ToDictionary(x => x.Key, x => x.Select(y => y.ValueId).Distinct().ToArray());

                    var featureValues = Model.Categories.Where(x => x.CategoryId == product.ParentCategoryId && x.FeatureId.HasValue)
                        .GroupBy(x => x.FeatureId.Value)
                        .Select(x => new { FetaureId = x.Key, FetureValueIds = x.Select(y => y.FeatureValueId.Value).ToArray() });

                    if (featureValues.Any(x => !productFeatures.TryGetValue(x.FetaureId, out int[] featureValues)
                                               || !featureValues.Intersect(x.FetureValueIds).Any()))
                    {
                        errors.Add($"Товар '{product.Name}' ({product.Id}) имеет другое значение характеристики");
                        continue;
                    }

                    int maxPriority = Model.Products.Select(x => x.Priority).DefaultIfEmpty(1).Max();

                    Model.Products.Add(new AssembledComputerRuleProductViewItem()
                    {
                        CategoryId = product.CategoryId,
                        ParentCategoryId = product.ParentCategoryId,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        QuantityFree = product.WarehouseQuantity,
                        Price = product.WarehousePrice,
                        Priority = maxPriority + 1
                    });
                }

                RefreshProducts();

                if (errors.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки при добавлении товаров", errors.Select(x => new ValidationResultItem(x, true)), this);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Товары добавлены успешно");
                }
            }
        }

        private void ReorderProductPriority()
        {
            DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
              new ReorderItemsParameter("Приоритет товаров", Products.OrderBy(x => x.Priority).Select(x => new ComboBoxItem(x.ProductId, $"{x.ProductName}")).ToList(), false, ReorderPriorityHandleOkAsync),
              this);
        }

        private void ClearBaseProduct()
        {
            Model.BaseProductId = null;
            Model.BaseProductName = null;
        }

        private async Task SelectBaseProductAsync()
        {
            List<AssembledComputerRuleDto> assembledComputerRules = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRules());

            List<ComboBoxItem> rules = assembledComputerRules
                .Where(x => x.Active && x.Id != Model.Id)
                .Select(x => new ComboBoxItem(x.ProductId, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(rules, "Выбор базы", "Конфигурация"), this);

            if (viewModel.IsOk)
            {
                Model.BaseProductId = viewModel.SelectedItem.Value.Id;
                Model.BaseProductName = viewModel.SelectedItem.Value.DisplayValue;
            }
        }

        private Task<bool> ReorderPriorityHandleOkAsync(IReadOnlyCollection<ComboBoxItem> changedPriorities)
        {
            Dictionary<int, int> reorderedItems = changedPriorities
              .Select((x, i) => new { x.Id, Priority = i })
              .ToDictionary(x => x.Id, x => x.Priority);

            bool isChanged = false;

            foreach (AssembledComputerRuleProductViewItem product in Model.Products)
            {
                if (reorderedItems.TryGetValue(product.ProductId, out int priority) && product.Priority != priority)
                {
                    product.Priority = priority;
                    isChanged = true;
                }
            }

            RefreshProducts();

            if (isChanged)
            {
                MessageFacadeService.ShowNotificationInfo("Приоритеты товаров успешно изменены");
            }

            return Task.FromResult(true);
        }

        private void RefreshProducts()
        {
            if (Model == null || SelectedCategory == null)
            {
                Products = null;
            }
            else
            {
                Products = Model.Products
                    .Where(x => x.ParentCategoryId == SelectedCategory.CategoryId)
                    .OrderBy(x => x.Priority)
                    .ToReadOnlyObservableCollection();
            }
        }

        private void OnCategoriesCellValueChanged(CellValueChangedEventArgs args)
        {
            if (args.Column.FieldName == nameof(AssembledComputerRuleCategoryViewItem.UseAnyProducts))
            {
                foreach (AssembledComputerRuleCategoryViewItem categoryViewItem in Model.Categories.Where(x => x.CategoryId == SelectedCategory.CategoryId))
                {
                    categoryViewItem.UseAnyProducts = SelectedCategory.UseAnyProducts;
                }
            }
            else if (args.Column.FieldName == nameof(AssembledComputerRuleCategoryViewItem.Quantity))
            {
                foreach (AssembledComputerRuleCategoryViewItem categoryViewItem in Model.Categories.Where(x => x.CategoryId == SelectedCategory.CategoryId))
                {
                    categoryViewItem.Quantity = SelectedCategory.Quantity;
                }
            }
        }

        private void CategoryProductTypesChanged(object sender, ProductTypesChangedEventArgs e)
        {
            if (e.CategoryId == SelectedCategory.CategoryId)
            {
                foreach (AssembledComputerRuleCategoryViewItem categoryViewItem in Model.Categories.Where(x => x.CategoryId == SelectedCategory.CategoryId))
                {
                    categoryViewItem.ProductTypes = SelectedCategory.ProductTypes;
                }
            }
        }

        private async Task AssembledComputerRuleCopyAsync()
        {
            AssembledComputerRuleCopyViewModel copyModel = DialogDocumentManagerService.ShowView<AssembledComputerRuleCopyViewModel>(null, this);

            if (copyModel.IsOk)
            {
                AssembledComputerRulesViewItem selectedAssembledComputerRules = copyModel.SelectedAssembledComputerRule;

                AssembledComputerRuleDto selectedAssemblyComputerRuleDto = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new QueryAssembledComputerRule(selectedAssembledComputerRules.Id)),
                    "получении правила конфигурации",
                    null,
                    this,
                    true,
                    showNotification: false);

                AssembledComputerRuleViewItem selectedAssemblyComputerRule = Mapper.Map<AssembledComputerRuleViewItem>(selectedAssemblyComputerRuleDto);

                if (copyModel.IsCopyCategory)
                {
                    Model.Categories.Clear();

                    Model.Categories.AddRange(selectedAssemblyComputerRule.Categories);

                    Model.Categories.ForEach(x => x.ProductTypesChanged += CategoryProductTypesChanged);

                    Model.Products.Clear();
                }

                if (copyModel.IsCopyProduct)
                {
                    Model.Products.AddRange(selectedAssemblyComputerRule.Products);
                }

                RefreshProducts();

                RefreshProductInfo();
            }
        }

        private async Task CheckAsync()
        {
            if (Model.Id == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Сохраните конфигурацию ПК");

                return;
            }

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter(
                "Максимальная цена конфигурации",
                "Задайте максимальную цену конфигурации",
                @"^[1-9]\d{0,9}$",
                "Значение не валидно"),
                this);

            if (viewModel.IsOk)
            {
                SelectComplectationGeneratorOptionsViewModel complectationGeneratorOptionsViewModel = DialogDocumentManagerService.ShowView<SelectComplectationGeneratorOptionsViewModel>(null, this);

                if (!complectationGeneratorOptionsViewModel.IsOk)
                {
                    return;
                }

                ComplactationGeneratorOptionsItem generatorOptions = complectationGeneratorOptionsViewModel.Model;

                ComplectationsGeneratorDto result;

                try
                {
                    int price = int.Parse(viewModel.Content);

                    decimal maxPrice = price + (price * (decimal)(generatorOptions.OverPricePercent / 100));

                    StockStrategy? strategy = generatorOptions.UseWarehouse == false ? StockStrategy.None : null;

                    result = await WebClient.ExecuteCatalogApiRequestAsync(
                        new GenerateConfigurationsByRules(maxPrice, true, strategy, generatorOptions.UseTransits, generatorOptions.UsePurchases, Model.Id));
                }
                catch
                {
                    MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
                    throw;
                }

                if (result?.Complectations?.FirstOrDefault()?.Complectation?.Any() == true)
                {
                    IReadOnlyCollection<ProductComplectationDto> complectation = result.Complectations.First().Complectation;

                    int[] productIds = complectation.Select(x => x.ProductId).ToArray();

                    List<ProductSimpleDto> products = await WebClient.ExecuteApiRequestAsync(new QueryProductsSimple(productIds));

                    IReadOnlyDictionary<int, string> productNames = products.ToDictionary(x => x.Id, x => x.GetLocalName(LocalizableNameType.Ukr));

                    GeneratedAssembledComputerRuleProductViewItem[] productsToShow = complectation.Select(x => new GeneratedAssembledComputerRuleProductViewItem()
                    {
                        ProductId = x.ProductId,
                        ProductName = productNames[x.ProductId],
                        Quantity = x.Quantity,
                        PriceUah = x.PriceUah.HasValue
                            ? Math.Round(x.PriceUah.Value, 2)
                            : null
                    }).ToArray();

                    DialogDocumentManagerService.ShowView<GeneratedAssembledComputerRuleViewModel>(
                        new GeneratedAssembledComputerRuleParameter(productsToShow), this);
                }
                else
                {
                    PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

                    IReadOnlyDictionary<int, string> categoryNames = categories.Data.ToDictionary(x => x.Id, x => x.Name);

                    List<ValidationResultItem> errors = new List<ValidationResultItem>();

                    if (result.DebugData.FirstOrDefault()?.FirstOrDefault(x => x.Value<string>("Message") == "After filter products by features")
                        is JObject afterFilterByFeatures)
                    {
                        if (afterFilterByFeatures.Property("Data")?.Value is JObject dataProperty)
                        {
                            int[] emptyCategoryIds = dataProperty.Properties()
                                .Where(x => int.TryParse(x.Name, out int _) && x.Value is JArray { Count: 0 })
                                .Select(x => int.Parse(x.Name))
                                .ToArray();

                            if (emptyCategoryIds.Any())
                            {
                                errors.Add(new ValidationResultItem(
                                    $"После фильтрации товаров по правилу, в категориях: {string.Join(", ", emptyCategoryIds.Select(x => categoryNames.GetValueOrDefault(x)))} не нашлось подходящих товаров",
                                    true));
                            }
                        }
                    }

                    JObject afterFilterByStocks = result.DebugData.FirstOrDefault()?
                        .FirstOrDefault(x => x.Value<string>("Message") == "After sort and filter products") as JObject;

                    if (afterFilterByStocks?.Property("Data")?.Value is JObject afterFilterByStocksData)
                    {
                        int[] emptyCategoryIds = afterFilterByStocksData.Properties()
                            .Where(x => int.TryParse(x.Name, out int _) && x.Value is JArray { Count: 0 })
                            .Select(x => int.Parse(x.Name))
                            .ToArray();

                        if (emptyCategoryIds.Any())
                        {
                            errors.Add(new ValidationResultItem(
                                $"После фильтрации товаров по свободным остаткам, в категориях: {string.Join(", ", emptyCategoryIds.Select(x => categoryNames.GetValueOrDefault(x)))} не нашлось подходящих товаров",
                                true));
                        }
                    }

                    JObject incompatibleProducts = result.DebugData.FirstOrDefault()?
                        .FirstOrDefault(x => x.Value<string>("Message") == "Incompatible products") as JObject;

                    if (incompatibleProducts?.Property("Data")?.Value is JArray jArray)
                    {
                        List<(int, int, string)> productsWithMessages = new List<(int, int, string)>();

                        foreach (JToken jToken in jArray)
                        {
                            if (jToken is JObject jObject && int.TryParse(jObject.Value<string>("product_id"), out int productId))
                            {
                                if (jObject.Property("incompatibility_data")?.Value is JArray incompatibilityDataJArray)
                                {
                                    foreach (JToken token in incompatibilityDataJArray)
                                    {
                                        if (token is JObject incompatibilityDataJObject
                                            && int.TryParse(incompatibilityDataJObject.Value<string>("incompatible_with_product_id"), out int incompatibleWithProductId))
                                        {
                                            string message = incompatibilityDataJObject.Property("messages")?.Value is JArray messagesJArray
                                                ? string.Join(", ", messagesJArray.Select(x => x.Value<string>()))
                                                : null;

                                            if (string.IsNullOrWhiteSpace(message?.Trim('"')))
                                            {
                                                message = jObject.Property("error_messages")?.Value is JArray errorMessagesJArray
                                                    ? string.Join(", ", errorMessagesJArray.Select(x => x.Value<string>()))
                                                    : null;
                                            }

                                            productsWithMessages.Add((productId, incompatibleWithProductId, message));
                                        }
                                    }
                                }
                            }
                        }

                        if (productsWithMessages.Any())
                        {
                            int[] productIds = productsWithMessages.Select(x => x.Item1)
                                .Union(productsWithMessages.Select(x => x.Item2))
                                .ToArray();

                            List<ProductSimpleDto> products = await WebClient.ExecuteApiRequestAsync(new QueryProductsSimple(productIds));

                            IReadOnlyDictionary<int, string> productNames = products.ToDictionary(x => x.Id, x => x.NameFullRu);

                            foreach ((int productId, int incompatibleProductId, string message) in productsWithMessages)
                            {
                                errors.Add(new ValidationResultItem($"Товар \"{productNames.GetValueOrDefault(productId)}\" не совместим с товаром \"{productNames.GetValueOrDefault(incompatibleProductId)}\": {message}", true));
                            }
                        }
                    }

                    JToken checkCompatibilityTimeOut = result.DebugData.FirstOrDefault()?
                        .FirstOrDefault(x => x.Value<string>("Message") == "Check compatibility timed out");

                    if (checkCompatibilityTimeOut != null)
                    {
                        errors.Add(new ValidationResultItem("Проверка товаров на совместимость прервалась из-за timeout", true));
                    }

                    JObject potentionalConfigurations = result.DebugData.FirstOrDefault()?
                        .FirstOrDefault(x => x.Value<string>("Message") == "Configurations generated") as JObject;

                    if (potentionalConfigurations != null)
                    {
                        errors.Add(new ValidationResultItem($"Потенциальных конфигураций: {potentionalConfigurations.Property("Data")?.Value?.Value<string>("ComplectationCount")}", false));
                    }
                    else
                    {
                        errors.Add(new ValidationResultItem("Не найдены товары, удовлетворяющие требованиям правила", true));
                    }

                    JToken generationTimeout = result.DebugData.FirstOrDefault()?
                        .FirstOrDefault(x => x.Value<string>("Message") == "Configuration generation timed out");

                    if (generationTimeout != null)
                    {
                        errors.Add(new ValidationResultItem("Генерация прервалась из-за timeout", true));
                    }

                    if (errors.Any())
                    {
                        if (errors.Count(x => x.IsError) == 0)
                        {
                            errors.Add(new ValidationResultItem($"Не удалось сгенерировать конфигурацию дешевле либо равной сумме {viewModel.Content} грн.", false));
                        }

                        MessageFacadeService.ShowValidationResultView("Ошибки при генерации конфигурации", errors, this);
                    }
                }
            }
        }
    }
}