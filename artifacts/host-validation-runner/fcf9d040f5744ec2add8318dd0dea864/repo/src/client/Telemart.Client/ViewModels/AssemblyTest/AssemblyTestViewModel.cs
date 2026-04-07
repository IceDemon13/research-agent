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
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssemblyTest;
using Telemart.Client.Data.Requests.Features.AssemblyTest.Actions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.AssemblyTest
{
    public sealed class AssemblyTestViewModel : TelemartEditorViewModelBase<AssemblyTestDto, AssemblyTestParameter, AssemblyTestViewItem>
    {
        public AssemblyTestViewModel(
          IWebClient webClient,
          IMessageFacadeService messageFacadeService,
          IMessenger messenger,
          IMapper mapper,
          IDictionaries dictionaries)
          : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddSlaveFeatureCommand = new DelegateCommand(AddSlaveFeature);
            DeleteSlaveFeatureCommand = new DelegateCommand(DeleteSlaveFeature, () => SelectedSlaveCategory != null);

            FeatureValidateCommand = new DelegateCommand<ValidationEventArgs>(FeatureValidate);
            CategoryValidateCommand = new DelegateCommand<ValidationEventArgs>(CategoryValidate);
        }

        public AssemblyTestViewModel()
        {
        }

        public IDelegateCommand AddSlaveFeatureCommand { get; }

        public IDelegateCommand DeleteSlaveFeatureCommand { get; }

        public IDelegateCommand FeatureValidateCommand { get; }

        public IDelegateCommand CategoryValidateCommand { get; }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> MainCategories
        {
            get { return GetProperty(() => MainCategories); }
            private set { SetProperty(() => MainCategories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Features
        {
            get { return GetProperty(() => Features); }
            private set { SetProperty(() => Features, value); }
        }

        public ReadOnlyObservableCollection<HierarchicalItem> MainFeatures
        {
            get { return GetProperty(() => MainFeatures); }
            set { SetProperty(() => MainFeatures, value); }
        }

        public AssemblyTestSlaveCategoryViewItem SelectedSlaveCategory
        {
            get { return GetProperty(() => SelectedSlaveCategory); }
            set { SetProperty(() => SelectedSlaveCategory, value); }
        }

        #region DialogSettings

        public override int Width => 1280;

        public override int MinWidth => 720;

        public override int MaxWidth => 1920;

        public override int Height => 720;

        public override int MinHeight => 576;

        public override int MaxHeight => 1080;

        #endregion

        #region BaseSettings

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Тест";

        protected override string UpdatedActionMessage => "сохранён";

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            PagedResult<FeatureGroupDto> featureGroups = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups());

            Categories = categories
                .OrderBy(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Features = featureGroups.Data
                .SelectMany(x => x.Features)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            MainCategories = categories
               .Where(x => x.ParentLevel <= 0 && x.Active > 0)
               .OrderBy(x => x.Position)
               .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            if (!IsNew)
            {
                RefreshMainFeatures(Model.MainCategoryId);
            }
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AssemblyTestViewItem.MainCategoryId):
                    MainFeatures = null;

                    RefreshMainFeatures(Model.MainCategoryId);

                    break;
            }
        }

        protected override Task<Result<AssemblyTestDto>> CreateEntityAsync()
        {
            AssemblyTestCreateDto createDto = MapCreateDto();

            return WebClient.ExecuteApiRequestAsync(new CreateAssemblyTest(createDto));
        }

        protected override Task<AssemblyTestDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAssemblyTest(id));
        }

        protected override void SetCreateTitle()
        {
            Title = $"Создание теста";
        }

        protected override void SetEditTitle()
        {
            Title = $"Тест ({Model.Id})";
        }

        protected override Task<LockResponse<AssemblyTestDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAssemblyTest(id));
        }

        protected override Task<LockResponse<AssemblyTestDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAssemblyTest(id));
        }

        protected override Task<Result<AssemblyTestDto>> UpdateEntityAsync()
        {
            AssemblyTestSaveDto saveDto = MapSaveDto(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateAssemblyTest(saveDto));
        }

        private static AssemblyTestSaveDto MapSaveDto(AssemblyTestViewItem model)
        {
            return new AssemblyTestSaveDto
            {
                Id = model.Id,
                Active = model.Active,
                AvailOnWeb = model.AvailOnWeb,
                MainCategoryId = model.MainCategoryId,
                MainFeatureIds = model.MainFeatureIds.ToArray(),
                Name = model.Name,
                NameUa = model.NameUa,
                NameEn = model.NameEn,
                Description = model.Description,
                Regex = model.Regex,
                Required = model.Required,
                Suffix = model.Suffix,
                SlaveCategories = model.SlaveCategories.Select(MapSaveDto).ToArray()
            };
        }

        private static AssemblyTestSlaveCategorySaveDto MapSaveDto(AssemblyTestSlaveCategoryViewItem model)
        {
            return new AssemblyTestSlaveCategorySaveDto
            {
                Id = model.Id,
                CategoryId = model.CategoryId,
                FeatureId = model.FeatureId
            };
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

        private void RefreshMainFeatures(int categoryId)
        {
            MainFeatures = MapFeatureItems(GetFeatures(categoryId)).ToReadOnlyObservableCollection();
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

        private void AddSlaveFeature()
        {
            GetFeatureFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetFeatureFromUserViewModel>(
                    new GetFeatureFromUserViewModelParameter("Выбор зависимой категории"), this);

            if (viewModel.IsOk)
            {
                if (Model.SlaveCategories.Any(x => x.CategoryId == viewModel.CategoryId && x.FeatureId == viewModel.Feature.Value.Id))
                {
                    MessageFacadeService.ShowNotificationWarning("Такая характеристика уже добавлена");
                    return;
                }

                Model.SlaveCategories.Add(new AssemblyTestSlaveCategoryViewItem(viewModel.CategoryId.Value, viewModel.Feature.Value.Id));
            }
        }

        private void DeleteSlaveFeature()
        {
            Model.SlaveCategories.Remove(SelectedSlaveCategory);
        }

        private AssemblyTestCreateDto MapCreateDto()
        {
            return new AssemblyTestCreateDto()
            {
                Active = Model.Active,
                AvailOnWeb = Model.AvailOnWeb,
                Required = Model.Required,
                Name = Model.Name,
                NameUa = Model.NameUa,
                NameEn = Model.NameEn,
                Description = Model.Description,
                Regex = Model.Regex,
                SlaveCategories = MapSlaveCategories().ToArray(),
                Suffix = Model.Suffix,
                GroupId = Model.GroupId,
                MainCategoryId = Model.MainCategoryId,
                MainFeatureIds = Model.MainFeatureIds.ToArray()
            };
        }

        private IEnumerable<AssemblyTestSlaveCategoryCreateDto> MapSlaveCategories()
        {
            foreach (AssemblyTestSlaveCategoryViewItem slaveCategory in Model.SlaveCategories)
            {
                yield return new AssemblyTestSlaveCategoryCreateDto()
                {
                    CategoryId = slaveCategory.CategoryId,
                    FeatureId = slaveCategory.FeatureId
                };
            }
        }

        private void FeatureValidate(ValidationEventArgs e)
        {
            IEnumerable<int> items = (e.Value as IEnumerable<object>)?.Cast<int>() ?? (e.Value as IEnumerable<int>);

            e.IsValid = items?.Any() == true && MainFeatures?.Any(x => x.ParentId == null && items.Contains(x.Id)) == false;

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
                CategoryDto parrentCategory = MainCategories.FirstOrDefault(x => x.Id == categoryId);

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