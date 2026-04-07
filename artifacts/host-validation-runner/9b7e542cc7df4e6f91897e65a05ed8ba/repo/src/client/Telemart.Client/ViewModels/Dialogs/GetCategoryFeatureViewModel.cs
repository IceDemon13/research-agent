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
    public sealed class GetCategoryFeatureViewModel : TelemartDialogViewModelBase
    {
        private GetCategoryFeatureParameter parameter;

        public GetCategoryFeatureViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
        }

        public GetCategoryFeatureViewModel()
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

        public bool SelectedCategoryEnabled
        {
            get { return GetProperty(() => SelectedCategoryEnabled); }
            private set { SetProperty(() => SelectedCategoryEnabled, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value, () => RaisePropertiesChanged(nameof(IsFeatureEnabled))); }
        }

        public HierarchicalItem? SelectedFeature
        {
            get { return GetProperty(() => SelectedFeature); }
            set { SetProperty(() => SelectedFeature, value); }
        }

        public bool IsFeatureEnabled => Features?.Any() == true;

        public static void BuildMetadata(MetadataBuilder<GetCategoryFeatureViewModel> builder)
        {
            builder.Property(x => x.SelectedCategory)
                .Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x == null || !y.parameter.ParentCategoryOnly || x.IsParent || y.parameter.CategoryId.HasValue, () => "Выберите родительскую категорию");

            builder.Property(x => x.SelectedFeature)
               .MatchesInstanceRule((x, y) => y.SelectedCategory == null || !y.parameter.AllRequired || (y.parameter.AllRequired && x.HasValue), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (GetCategoryFeatureParameter)Parameter;

            Title = "Выбор категории с характеристикой";

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

        private void SelectedCategoryChanged()
        {
            if (SelectedCategory != null)
            {
                RefreshFeatures(SelectedCategory.Id);
            }

            RaisePropertyChanged(nameof(SelectedFeature));
        }
    }
}