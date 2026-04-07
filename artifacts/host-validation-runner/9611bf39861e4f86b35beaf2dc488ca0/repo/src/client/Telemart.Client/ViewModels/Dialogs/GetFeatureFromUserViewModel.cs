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
    public sealed class GetFeatureFromUserViewModel : TelemartDialogViewModelBase
    {
        public GetFeatureFromUserViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
            CategoryValidateCommand = new DelegateCommand<ValidationEventArgs>(CategoryValidate);
        }

        public GetFeatureFromUserViewModel()
        {
        }

        public IDelegateCommand FeatureValidateCommand { get; }

        public IDelegateCommand CategoryValidateCommand { get; }

        public bool IsReadonlyCategory
        {
            get { return GetProperty(() => IsReadonlyCategory); }
            private set { SetProperty(() => IsReadonlyCategory, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public int? CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value, OnMasterCategoryIdChanged); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public HierarchicalItem? Feature
        {
            get { return GetProperty(() => Feature); }
            set { SetProperty(() => Feature, value); }
        }

        public static void BuildMetadata(MetadataBuilder<GetFeatureFromUserViewModel> builder)
        {
            builder.Property(x => x.CategoryId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Feature)
              .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            GetFeatureFromUserViewModelParameter parameter = (GetFeatureFromUserViewModelParameter)Parameter;

            Title = parameter.Title;

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            if (parameter.CategotyId == null)
            {
                Categories = categories
                    .Where(x => x.ParentLevel <= 0 && x.Active > 0)
                    .OrderBy(x => x.Position)
                    .ToReadOnlyObservableCollection();
            }
            else
            {
                IsReadonlyCategory = true;
                Categories = categories.Where(x => x.Id == parameter.CategotyId).ToReadOnlyObservableCollection();
                CategoryId = parameter.CategotyId;
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

        private void OnMasterCategoryIdChanged()
        {
            Features = null;

            if (CategoryId.HasValue)
            {
                RefreshMasterFeatures(CategoryId.Value);
            }
        }

        private void RefreshMasterFeatures(int categoryId)
        {
            Features = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
        }

        private IReadOnlyCollection<FeatureGroupDto> GetFeatures(int categoryId)
        {
            PagedResult<FeatureGroupDto> featureGroups = WebClient.ExecuteApiRequest(new QueryFeatureGroups(categoryId));

            if (!featureGroups.Data.Any(x => x.Features.Any()))
            {
                MessageFacadeService.ShowNotificationWarning("У выбранной категории нет характеристик");
                return Array.Empty<FeatureGroupDto>();
            }
            else
            {
                return featureGroups.Data;
            }
        }

        private void FeatureValidate(ValidationEventArgs e)
        {
            e.IsValid = e.Value is HierarchicalItem item && item.ParentId.HasValue;

            if (!e.IsValid)
            {
                e.ErrorType = ErrorType.Critical;
                e.ErrorContent = "Выберите характеристику, а не группу";
            }

            e.Handled = true;
        }

        private void CategoryValidate(ValidationEventArgs e)
        {
            if (e.Value != null && e.Value is int categoryId && Categories != null)
            {
                CategoryDto parrentCategory = Categories.FirstOrDefault(x => x.Id == categoryId);

                e.IsValid = parrentCategory != null && parrentCategory.IsParent;

                if (!e.IsValid)
                {
                    e.ErrorType = ErrorType.Critical;
                    e.ErrorContent = "Выберите родительскую категорию";
                }

                e.Handled = true;
            }
        }
    }
}