using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors.DXErrorProvider;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleCreateCategoryViewModel : TelemartDialogViewModelBase
    {
        private AssembledComputerRuleCreateCategoryParameter _parameter;

        public AssembledComputerRuleCreateCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
        }

        public AssembledComputerRuleCreateCategoryViewModel()
        {
        }

        public IDelegateCommand FeatureValidateCommand { get; }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public CategoryDto SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, SelectedCategoryChanged); }
        }

        public ReadOnlyObservableCollection<ProductType> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public List<object> SelectedProductTypeIds
        {
            get { return GetProperty(() => SelectedProductTypeIds); }
            set { SetProperty(() => SelectedProductTypeIds, value, SelectedProductTypeIdsChanged); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value, () => RaisePropertiesChanged(nameof(AnyFeatures))); }
        }

        public HierarchicalItem? SelectedFeature
        {
            get { return GetProperty(() => SelectedFeature); }
            set { SetProperty(() => SelectedFeature, value, SelectedFeatureChanged); }
        }

        public ReadOnlyObservableCollection<string> Operations
        {
            get { return GetProperty(() => Operations); }
            private set { SetProperty(() => Operations, value); }
        }

        public string SelectedOperation
        {
            get { return GetProperty(() => SelectedOperation); }
            set { SetProperty(() => SelectedOperation, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            private set { SetProperty(() => FeatureValues, value, () => RaisePropertiesChanged(nameof(IsFeatureValueEnabled))); }
        }

        public ComboBoxItem? SelectedFeatureValue
        {
            get { return GetProperty(() => SelectedFeatureValue); }
            set { SetProperty(() => SelectedFeatureValue, value); }
        }

        public bool AnyFeatures => Features?.Any() == true;

        public bool IsFeatureValueEnabled => FeatureValues?.Any() == true;

        public static void BuildMetadata(MetadataBuilder<AssembledComputerRuleCreateCategoryViewModel> builder)
        {
            builder.Property(x => x.SelectedOperation)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x == null || x.IsParent, () => "Выберите родительскую категорию");

            builder.Property(x => x.SelectedFeature)
               .MatchesInstanceRule((x, y) => y.SelectedCategory == null || x.HasValue || y.SelectedProductTypeIds?.Any() == true, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedFeatureValue)
               .MatchesInstanceRule((x, y) => !(x is null && y.SelectedFeature != null), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (AssembledComputerRuleCreateCategoryParameter)Parameter;

            Title = "Добавление категории в правило конфигурации";

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.Active > 0)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            ProductTypes = Dictionaries.GetItems<ProductType>()
                .Where(x => x.AllowedInAssembledComputerRule)
                .ToReadOnlyObservableCollection();

            Operations = new[]
            {
                "=",
                ">",
                "<",
                ">=",
                "<="
            }.ToReadOnlyObservableCollection();

            SelectedOperation = "=";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            if (!IDataErrorInfoHelper.HasErrors(this))
            {
                IsOk = true;
                Close();
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<HierarchicalItem> MapFeatureItems(IReadOnlyCollection<FeatureGroupDto> features)
        {
            foreach (FeatureGroupDto group in features.OrderBy(x => x.Position))
            {
                yield return new HierarchicalItem(group.Id, group.Name?.Trim());

                foreach (FeatureDto feature in group.Features.OrderBy(x => x.Position))
                {
                    yield return new HierarchicalItem(feature.Id, feature.Name?.Trim(), group.Id);
                }
            }
        }

        private static void FeatureValidate(ValidationEventArgs e)
        {
            e.IsValid = (e.Value is HierarchicalItem item && item.ParentId.HasValue) || e.Value is null;

            if (!e.IsValid)
            {
                e.ErrorType = ErrorType.Critical;
                e.ErrorContent = "Выберите характеристику, а не группу";
            }

            e.Handled = true;
        }

        private IReadOnlyCollection<FeatureGroupDto> GetFeatures(int categoryId)
        {
            PagedResult<FeatureGroupDto> featureGroups = WebClient.ExecuteApiRequest(new QueryFeatureGroups(categoryId));

            if (!featureGroups.Data.Any(x => x.Features.Any()))
            {
                MessageFacadeService.ShowNotificationWarning("У выбранной категории нет характеристик");

                return Array.Empty<FeatureGroupDto>();
            }

            return featureGroups.Data;
        }

        private void RefreshFeatures(int categoryId)
        {
            Features = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
        }

        private void RefreshFeatureValues(int featureId)
        {
            List<FeatureValueExDto> featureValues = WebClient.ExecuteApiRequest(new QueryFeatureValues(featureId));

            FeatureValues = featureValues
                .OrderBy(x => x.Value)
                .Select(x => new ComboBoxItem(x.Id, x.Value)).ToReadOnlyObservableCollection();
        }

        private void SelectedCategoryChanged()
        {
            FeatureValues = null;

            if (SelectedCategory != null)
            {
                RefreshFeatures(SelectedCategory.Id);

                SelectedProductTypeIds = _parameter.CategoryProductTypeIds.TryGetValue(SelectedCategory.Id, out int[] productTypeIds)
                    ? productTypeIds.Cast<object>().ToList()
                    : new List<object> { ProductType.ProductId };
            }

            RaisePropertyChanged(nameof(SelectedFeature));
        }

        private void SelectedFeatureChanged()
        {
            FeatureValues = null;

            if (SelectedFeature.HasValue)
            {
                RefreshFeatureValues(SelectedFeature.Value.Id);
            }

            RaisePropertyChanged(nameof(SelectedFeatureValue));
        }

        private void SelectedProductTypeIdsChanged()
        {
            RaisePropertyChanged(nameof(SelectedFeature));
        }
    }
}