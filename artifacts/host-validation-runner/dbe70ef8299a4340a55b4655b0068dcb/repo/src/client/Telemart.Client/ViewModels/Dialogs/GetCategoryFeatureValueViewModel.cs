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

namespace Telemart.Client.ViewModels.Dialogs
{
    public sealed class GetCategoryFeatureValueViewModel : TelemartDialogViewModelBase
    {
        private GetCategoryFeatureValueParameter parameter;

        public GetCategoryFeatureValueViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
            SelectAllFeatureValuesCommand = new DelegateCommand(SelectAllFeatureValues);
            SelectedFeatureValues = new List<object>();
        }

        public GetCategoryFeatureValueViewModel()
        {
        }

        public IDelegateCommand FeatureValidateCommand { get; }

        public IDelegateCommand SelectAllFeatureValuesCommand { get; }

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

        public bool SelectedCategoryEnabled
        {
            get { return GetProperty(() => SelectedCategoryEnabled); }
            private set { SetProperty(() => SelectedCategoryEnabled, value); }
        }

        public bool SelectedFeatureEnabled
        {
            get { return GetProperty(() => SelectedFeatureEnabled); }
            private set { SetProperty(() => SelectedFeatureEnabled, value); }
        }

        public bool MultiSelectFeatureValue
        {
            get { return GetProperty(() => MultiSelectFeatureValue); }
            private set { SetProperty(() => MultiSelectFeatureValue, value); }
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

        public List<object> SelectedFeatureValues
        {
            get { return GetProperty(() => SelectedFeatureValues); }
            set { SetProperty(() => SelectedFeatureValues, value); }
        }

        public bool AnyFeatures => Features?.Any() == true;

        public bool IsFeatureValueEnabled => FeatureValues?.Any() == true;

        public static void BuildMetadata(MetadataBuilder<GetCategoryFeatureValueViewModel> builder)
        {
            builder.Property(x => x.SelectedCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x == null || !y.parameter.ParentCategoryOnly || x.IsParent || y.parameter.CategoryId.HasValue, () => "Выберите родительскую категорию");

            builder.Property(x => x.SelectedFeature)
                .MatchesInstanceRule((x, y) => y.SelectedCategory == null || !y.parameter.AllRequired || (y.parameter.AllRequired && x.HasValue), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedFeatureValue)
                .MatchesInstanceRule((x, y) => !(x is null && y.SelectedFeature != null && y.MultiSelectFeatureValue == false), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedFeatureValues)
                .MatchesInstanceRule((_, y) => y.SelectedFeature is null || y.MultiSelectFeatureValue == false || y.SelectedFeatureValues?.Any() == true, () => Resources.RequiredErrorMessage);
        }

        public List<ComboBoxItem> GetSelectedFeatureValues()
        {
            return SelectedFeatureValues.Cast<ComboBoxItem>().ToList();
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (GetCategoryFeatureValueParameter)Parameter;

            MultiSelectFeatureValue = parameter.MultiSelectFeatureValue;

            Title = "Выбор категории со значением характеристик";

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.Active > 0)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            if (parameter.CategoryId.HasValue)
            {
                SelectedCategory = categories.First(x => x.Id == parameter.CategoryId.Value);
                SelectedCategoryEnabled = false;
            }
            else
            {
                SelectedCategoryEnabled = true;
            }

            if (parameter.FeatureId.HasValue && parameter.CategoryId.HasValue)
            {
                RefreshFeatures(SelectedCategory.Id);

                SelectedFeature = new HierarchicalItem(parameter.FeatureId.Value, parameter.FeatureName, 0);

                SelectedFeatureEnabled = false;
            }
            else
            {
                SelectedFeatureEnabled = true;
            }

            if (parameter.MultiSelectFeatureValue == false && parameter.FeatureValueId.HasValue && parameter.FeatureId.HasValue && parameter.CategoryId.HasValue)
            {
                if (FeatureValues == null)
                {
                    RefreshFeatureValues(parameter.FeatureId.Value);
                }

                SelectedFeatureValue = FeatureValues!.FirstOrDefault(x => x.Id == parameter.FeatureValueId.Value);
            }

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

        private void SelectAllFeatureValues()
        {
            SelectedFeatureValues = FeatureValues.Cast<object>().ToList();
            RaisePropertyChanged(nameof(SelectedFeatureValues));
        }

        private IReadOnlyCollection<FeatureGroupDto> GetFeatures(int categoryId)
        {
            if (parameter.ParentCategoryFeatures)
            {
                IEnumerable<CategoryDto> pathCategories = from node in Categories
                    from parent in Categories
                    where node.Left >= parent.Left && node.Left <= parent.Right && node.Id == categoryId
                    orderby parent.Left
                    select parent;

                CategoryDto category = pathCategories.FirstOrDefault(x => x.IsParent);

                if (category is not null)
                {
                    categoryId = category.Id;
                }
            }

            PagedResult<FeatureGroupDto> featureGroups = WebClient.ExecuteApiRequest(new QueryFeatureGroups(categoryId));

            if (!featureGroups.Data.Any(x => x.Features.Any()))
            {
                MessageFacadeService.ShowNotificationWarning("У выбранной категории нет характеристик");

                if (parameter.CategoryId.HasValue)
                {
                    Close();
                }

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
                .Where(x => parameter.ExcludedFeatureValueIds is null || !parameter.ExcludedFeatureValueIds.Contains(x.Id))
                .OrderBy(x => x.Value)
                .Select(x => new ComboBoxItem(x.Id, x.Value)).ToReadOnlyObservableCollection();
        }

        private void SelectedCategoryChanged()
        {
            FeatureValues = null;

            if (SelectedCategory != null)
            {
                RefreshFeatures(SelectedCategory.Id);
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
    }
}