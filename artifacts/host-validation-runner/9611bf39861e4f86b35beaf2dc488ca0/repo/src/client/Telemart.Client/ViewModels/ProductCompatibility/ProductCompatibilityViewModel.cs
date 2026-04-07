using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using DevExpress.XtraEditors.DXErrorProvider;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.ProductCompatibility;
using Telemart.Client.Data.Requests.Features.ProductCompatibility.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ProductCompatibility;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.ProductCompatibility
{
    public class ProductCompatibilityViewModel : TelemartEditorViewModelBase<ProductCompatibilityDto, ProductCompatibilityParameter, ProductCompatibilityViewItem>
    {
        public ProductCompatibilityViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
            CategoryValidateCommand = new DelegateCommand<ValidationEventArgs>(CategoryValidate);
        }

        public IDelegateCommand FeatureValidateCommand { get; }

        public IDelegateCommand CategoryValidateCommand { get; }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<string> CompareMethods
        {
            get { return GetProperty(() => CompareMethods); }
            private set { SetProperty(() => CompareMethods, value); }
        }

        public ReadOnlyObservableCollection<NotificationImage> NotificationImages
        {
            get { return GetProperty(() => NotificationImages); }
            private set { SetProperty(() => NotificationImages, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> MasterFeatures
        {
            get { return GetProperty(() => MasterFeatures); }
            private set { SetProperty(() => MasterFeatures, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> SlaveFeatures
        {
            get { return GetProperty(() => SlaveFeatures); }
            private set { SetProperty(() => SlaveFeatures, value); }
        }

        public string MasterProductLabel
        {
            get { return GetProperty(() => MasterProductLabel); }
            private set { SetProperty(() => MasterProductLabel, value); }
        }

        public string SlaveProductLabel
        {
            get { return GetProperty(() => SlaveProductLabel); }
            private set { SetProperty(() => SlaveProductLabel, value); }
        }

        public bool IsAssemblyRule => Model != null && Model.TypeId == ProductCompatibilityType.PC.Id;

        protected override string CreatedActionMessage { get; } = "создано";

        protected override string EntityName { get; } = "Правило";

        protected override string UpdatedActionMessage { get; } = "сохранено";

        protected override void OnInitializeInDesignMode()
        {
            MasterProductLabel = "Ограничивающий товар";
            SlaveProductLabel = "Ограничиваемый товар";
        }

        protected override async Task HandleLoadedAsync()
        {
            ProductCompatibilityParameter parameter = (ProductCompatibilityParameter)Parameter;

            MasterProductLabel = "Ограничивающий товар";

            SlaveProductLabel = parameter.TypeId == ProductCompatibilityType.PC.Id
                ? "Ограничиваемый товар"
                : "Ограничиваемый аксессуар";

            CompareMethods = new[] { "=", ">", ">=", "<", "<=" }.ToReadOnlyObservableCollection();

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Where(x => x.ParentLevel <= 0 && x.Active > 0)
                .OrderBy(x => x.Position)
                .ToReadOnlyObservableCollection();

            NotificationImages = Dictionaries.GetItems<NotificationImage>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            if (Model.MasterCategoryId.HasValue)
            {
                RefreshMasterFeatures(Model.MasterCategoryId.Value);
            }

            if (Model.SlaveCategoryId.HasValue)
            {
                RefreshSlaveFeatures(Model.SlaveCategoryId.Value);
            }
        }

        protected override void AfterSetData()
        {
            RaisePropertyChanged(nameof(IsAssemblyRule));
        }

        protected override Task<Result<ProductCompatibilityDto>> CreateEntityAsync()
        {
            ProductCompatibilitySaveDto dto = Mapper.Map<ProductCompatibilitySaveDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new CreateProductCompatibility(dto));
        }

        protected override object CreateEntityMessage(ProductCompatibilityDto dto, MessageType messageType)
        {
            return new AssemblyCompatibilityMessage(dto, messageType);
        }

        protected override Task<ProductCompatibilityDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryProductCompatibility(id));
        }

        protected override Task<LockResponse<ProductCompatibilityDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockProductCompatibility(id));
        }

        protected override Task<LockResponse<ProductCompatibilityDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockProductCompatibility(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила совместимости";
        }

        protected override void SetEditTitle()
        {
            Title = $"Правило совместимости №{Model.Id}";
        }

        protected override Task<Result<ProductCompatibilityDto>> UpdateEntityAsync()
        {
            ProductCompatibilitySaveDto dto = Mapper.Map<ProductCompatibilitySaveDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateProductCompatibility(dto));
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ProductCompatibilityViewItem.MasterCategoryId):
                    MasterFeatures = null;

                    if (Model.MasterCategoryId.HasValue)
                    {
                        RefreshMasterFeatures(Model.MasterCategoryId.Value);
                    }

                    break;
                case nameof(ProductCompatibilityViewItem.SlaveCategoryId):
                    SlaveFeatures = null;

                    if (Model.SlaveCategoryId.HasValue)
                    {
                        RefreshSlaveFeatures(Model.SlaveCategoryId.Value);
                    }

                    break;
            }
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

        private void RefreshMasterFeatures(int categoryId)
        {
            MasterFeatures = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
        }

        private void RefreshSlaveFeatures(int categoryId)
        {
            SlaveFeatures = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
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