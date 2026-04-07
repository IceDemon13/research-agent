using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PromoCode;
using Telemart.Client.Data.Requests.Features.PromoCode.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PromoCode;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeViewModel : TelemartEditorViewModelBase<PromoCodeFullDto, PromoCodeParameter, PromoCodeFullViewItem>
    {
        private readonly IErrorHandler _errorHandler;
        private IReadOnlyDictionary<int, string> _employeeNames;

        public PromoCodeViewModel(
            IErrorHandler errorHandler,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _errorHandler = errorHandler;
            DeleteProductCommand = new DelegateCommand<PromoCodeProductViewItem>(DeleteProduct, x => x != null);
            AddProductCommand = new DelegateCommand(AddProduct);
            AddCategoryCommand = new DelegateCommand(AddCategory);
            AddBundleProductCommand = new DelegateCommand(AddBundleProduct, () => AllowEditAllData);
            AddBundleCategoryCommand = new DelegateCommand(AddBundleCategory, () => AllowEditAllData);
            EditBundleCategoryCommand = new DelegateCommand(EditBundleCategory, () => AllowEditAllData);
            DeleteBundleCategoryCommand = new DelegateCommand(() => Model.BundleCategories.Remove(SelectedBundleCategory), () => SelectedBundleCategory != null && AllowEditAllData);
            DeleteBundleProductCommand = new DelegateCommand(() => Model.BundleProducts.Remove(SelectedBundleProduct), () => SelectedBundleProduct != null && AllowEditAllData);
            GeneratePromoCodeCommand = new DelegateCommand(GeneratePromoCode, () => IsNew);
            OnCategoriesCellValueChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(OnCategoriesCellValueChanged);
            CleanCacheDataCommand = new AsyncCommand(CleanCacheDataAsync, () => Model?.Id > 0 && WebClient.IsOperationAllowed(BusinessOperation.PromoCodeBundleClearCache) && !IsLockedByCurrentEmployee);
        }

        public IDelegateCommand DeleteProductCommand { get; }

        public IDelegateCommand AddProductCommand { get; }

        public IDelegateCommand GeneratePromoCodeCommand { get; }

        public IDelegateCommand AddCategoryCommand { get; }

        public IDelegateCommand AddBundleCategoryCommand { get; }

        public IDelegateCommand EditBundleCategoryCommand { get; }

        public IDelegateCommand DeleteBundleCategoryCommand { get; }

        public IDelegateCommand AddBundleProductCommand { get; }

        public IDelegateCommand DeleteBundleProductCommand { get; }

        public IDelegateCommand OnCategoriesCellValueChangedCommand { get; }

        public IAsyncCommand CleanCacheDataCommand { get; }

        #region DialogSettings

        public override int Height => 770;

        public override int MaxHeight => 1080;

        public override int MinHeight => 690;

        public override int MaxWidth => 1920;

        public override int MinWidth => 750;

        public override int Width => 1000;

        #endregion

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public PromoCodeBundleCategoryViewItem SelectedBundleCategory
        {
            get { return GetProperty(() => SelectedBundleCategory); }
            set { SetProperty(() => SelectedBundleCategory, value); }
        }

        public PromoCodeBundleProductViewItem SelectedBundleProduct
        {
            get { return GetProperty(() => SelectedBundleProduct); }
            set { SetProperty(() => SelectedBundleProduct, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeDiscountMode> ProductDiscountModes
        {
            get { return GetProperty(() => ProductDiscountModes); }
            private set { SetProperty(() => ProductDiscountModes, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeDiscountMode> CategoryDiscountModes
        {
            get { return GetProperty(() => CategoryDiscountModes); }
            private set { SetProperty(() => CategoryDiscountModes, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryViewItems
        {
            get { return GetProperty(() => SummaryViewItems); }
            private set { SetProperty(() => SummaryViewItems, value); }
        }

        public ReadOnlyObservableCollection<PromoCodeType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public bool AllowEdit
        {
            get { return GetProperty(() => AllowEdit); }
            private set { SetProperty(() => AllowEdit, value); }
        }

        public bool AllowEditAllData
        {
            get { return GetProperty(() => AllowEditAllData); }
            private set { SetProperty(() => AllowEditAllData, value); }
        }

        public bool IsBundlesBlockVisible
        {
            get { return GetProperty(() => IsBundlesBlockVisible); }
            private set { SetProperty(() => IsBundlesBlockVisible, value); }
        }

        public bool IsProductsBlockVisible
        {
            get { return GetProperty(() => IsProductsBlockVisible); }
            private set { SetProperty(() => IsProductsBlockVisible, value); }
        }

        protected override string CreatedActionMessage { get; } = "создана";

        protected override string EntityName { get; } = "Акция";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        protected override async Task HandleLoadedAsync()
        {
            ProductDiscountModes = Dictionaries.GetItems<PromoCodeDiscountMode>().ToReadOnlyObservableCollection();
            Types = Dictionaries.GetItems<PromoCodeType>().ToReadOnlyObservableCollection();
            CategoryDiscountModes = ProductDiscountModes.Where(x => x.Id != PromoCodeDiscountMode.Price.Id).ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshCategoriesAsync(), RefreshEmployeesAsync());

            await base.HandleLoadedAsync();

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                _employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshCategoriesAsync()
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        protected override Task<Result<PromoCodeFullDto>> CreateEntityAsync()
        {
            PromoCodeSaveDto dto = Mapper.Map<PromoCodeSaveDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new CreatePromoCode(dto));
        }

        protected override object CreateEntityMessage(PromoCodeFullDto dto, MessageType messageType)
        {
            return new PromoCodeMessage(dto, messageType);
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Model.TypeId):

                    RecalculateBlockVisibilities();

                    break;
            }
        }

        protected override Task<PromoCodeFullDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryPromoCode(id));
        }

        protected override Task<LockResponse<PromoCodeFullDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockPromoCode(id));
        }

        protected override Task<LockResponse<PromoCodeFullDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockPromoCode(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Добавление акции";

            Model.ShowInSite = true;
            Model.DateStart = null;
            Model.DateEnd = null;
        }

        protected override void SetEditTitle()
        {
            Title = $"Акция {Model.Value} ({Model.Id})";
        }

        protected override Task<Result<PromoCodeFullDto>> UpdateEntityAsync()
        {
            PromoCodeSaveDto dto = Mapper.Map<PromoCodeSaveDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdatePromoCode(dto));
        }

        protected override bool CanEdit()
        {
            return base.CanEdit()
                   && WebClient.IsOperationAllowed(BusinessOperation.PromoCodeUpdate);
        }

        protected override void AfterSetData()
        {
            AllowEdit = IsNew || (Model.DateStart > DateTime.Now && IsLockedByCurrentEmployee);

            AllowEditAllData = AllowEdit && !Model.UsedInOrders;

            if (IsNew)
            {
                Model.TypeId = PromoCodeType.ProductDiscount.Id;
            }
            else
            {
                SummaryViewItems = GetSummaryViewItem();
            }

            RecalculateBlockVisibilities();

            base.AfterSetData();
        }

        private void GeneratePromoCode()
        {
            string[] adjectiveWords = new[] { "SIMPLE", "PRO", "TURBO", "4K", "FUN", "HOT", "ICE", "GUILTY", "LAZY", "STRONG", "CALM", "HARD", "SOFT", "STORM", "GOLD"};
            string[] nounWords = new[] { "GAME", "DEVIL", "PHOENIX", "BULLET", "RACER", "MASTER", "SPACE", "ROCKET", "THUNDER", "MOON", "GALAXY", "FUSION", "SPIDER", "WOLF", "WAVE" };

            Model.Value = $"{adjectiveWords[Random.Shared.Next(0, 15)]}-{nounWords[Random.Shared.Next(0, 15)]}-{Random.Shared.Next(1, 10)}";
        }

        private void OnCategoriesCellValueChanged(CellValueChangedEventArgs args)
        {
            if (args.Column.FieldName == nameof(PromoCodeBundleCategoryViewItem.Quantity))
            {
                foreach (PromoCodeBundleCategoryViewItem categoryViewItem in Model.BundleCategories.Where(x => x.CategoryId == SelectedBundleCategory.CategoryId))
                {
                    categoryViewItem.Quantity = SelectedBundleCategory.Quantity;
                }
            }
            else if (args.Column.FieldName == nameof(PromoCodeBundleCategoryViewItem.DiscountModeId))
            {
                foreach (PromoCodeBundleCategoryViewItem categoryViewItem in Model.BundleCategories.Where(x => x.CategoryId == SelectedBundleCategory.CategoryId))
                {
                    categoryViewItem.DiscountModeId = SelectedBundleCategory.DiscountModeId;
                }
            }
            else if (args.Column.FieldName == nameof(PromoCodeBundleCategoryViewItem.DiscountAmount))
            {
                foreach (PromoCodeBundleCategoryViewItem categoryViewItem in Model.BundleCategories.Where(x => x.CategoryId == SelectedBundleCategory.CategoryId))
                {
                    categoryViewItem.DiscountAmount = SelectedBundleCategory.DiscountAmount;
                }
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryViewItem()
        {
            yield return new SummaryViewItem("Создан", $"{_employeeNames.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменен", $"{_employeeNames.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
        }

        private void DeleteProduct(PromoCodeProductViewItem item)
        {
            Model.Products.Remove(item);
        }

        private void AddBundleProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByCheck,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                Model.BundleProducts.AddRange(nomenclatureViewModel.GetSelectedItems().Select(x => new PromoCodeBundleProductViewItem { ProductId = x.Id, ProductName = x.Name, Quantity = 1 }));
            }
        }

        private void AddBundleCategory()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(new GetCategoryFeatureValueParameter(false, false), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SelectedFeatureValue is null && Model.BundleCategories.Where(x => !x.FeatureValueId.HasValue).FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория уже добавлена");
                return;
            }

            if (Model.BundleCategories.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id && x.FeatureId == viewModel.SelectedFeature?.Id && x.FeatureValueId == viewModel.SelectedFeatureValue?.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Значение характеристики уже добавлено");
                return;
            }

            var existedCategory = Model.BundleCategories.FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id);

            Model.BundleCategories.Add(new PromoCodeBundleCategoryViewItem
            {
                CategoryId = viewModel.SelectedCategory.Id,
                FeatureId = viewModel.SelectedFeature?.Id,
                FeatureName = viewModel.SelectedFeature?.DisplayValue,
                FeatureValueId = viewModel.SelectedFeatureValue?.Id,
                FeatureValueName = viewModel.SelectedFeatureValue?.DisplayValue,
                Quantity = existedCategory?.Quantity ?? 1,
                DiscountAmount = existedCategory?.DiscountAmount,
                DiscountModeId = existedCategory?.DiscountModeId
            });
        }

        private void EditBundleCategory()
        {
            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(
                new GetCategoryFeatureValueParameter(
                    false,
                    false,
                    categoryId: SelectedBundleCategory.CategoryId,
                    featureId: SelectedBundleCategory.FeatureId,
                    featureName: SelectedBundleCategory.FeatureName,
                    featureValueId: SelectedBundleCategory.FeatureValueId),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SelectedFeatureValue is null && Model.BundleCategories
                    .Where(x => x != SelectedBundleCategory && !x.FeatureValueId.HasValue)
                    .FirstOrDefault(x => x.CategoryId == viewModel.SelectedCategory.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Категория уже добавлена");
                return;
            }

            if (Model.BundleCategories.FirstOrDefault(x => x != SelectedBundleCategory
                                                           && x.CategoryId == viewModel.SelectedCategory.Id
                                                           && x.FeatureId == viewModel.SelectedFeature?.Id
                                                           && x.FeatureValueId == viewModel.SelectedFeatureValue?.Id) != null)
            {
                MessageFacadeService.ShowNotificationWarning("Значение характеристики уже добавлено");
                return;
            }

            SelectedBundleCategory.CategoryId = viewModel.SelectedCategory.Id;
            SelectedBundleCategory.FeatureId = viewModel.SelectedFeature?.Id;
            SelectedBundleCategory.FeatureName = viewModel.SelectedFeature?.DisplayValue;
            SelectedBundleCategory.FeatureValueId = viewModel.SelectedFeatureValue?.Id;
            SelectedBundleCategory.FeatureValueName = viewModel.SelectedFeatureValue?.DisplayValue;
        }

        private void AddProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.ByCheck,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                Model.Products.AddRange(nomenclatureViewModel.GetSelectedItems().Select(x => new PromoCodeProductViewItem { ProductId = x.Id, ProductName = x.Name }));
            }
        }

        private void AddCategory()
        {
            ChooseCategoryViewModel result = DialogDocumentManagerService.ShowView<ChooseCategoryViewModel>(new ChooseCategoryParameter(false, null), this);

            if (result.IsOk && result.SelectedCategory != null)
            {
                Model.Products.Add(new PromoCodeProductViewItem { CategoryId = result.SelectedCategory.Id });
            }
        }

        private async Task CleanCacheDataAsync()
        {
            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CleanPromoCodeCache(Model.Id)),
                "очистке кэша",
                "Кэш очищен",
                null,
                true,
                showNotification: true);
        }

        private void RecalculateBlockVisibilities()
        {
            IsBundlesBlockVisible = Model.TypeId == PromoCodeType.Bundle.Id;

            IsProductsBlockVisible = Model.TypeId == PromoCodeType.ProductDiscount.Id;
        }
    }
}