using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Accessory;
using Telemart.Client.Data.Requests.Features.Accessory.Action;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Accessory;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryViewModel : TelemartEditorViewModelBase<AccessoryDto, AccessoryParameter, AccessoryViewItem>
    {
        public AccessoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            AddCategoryCommand = new DelegateCommand(AddCategory);
            RemoveCategoryCommand = new DelegateCommand(RemoveCategory, () => SelectedCategory is not null);
            AddFeatureCommand = new DelegateCommand(AddFeature, () => Model?.CategoryId is not null);
            RemoveFeatureCommand = new DelegateCommand(RemoveFeature, () => SelectedFeature is not null);
            ShowCategoryFeaturesCommand = new DelegateCommand(ShowCategoryFeatures, () => SelectedCategory is not null);
        }

        public IDelegateCommand AddCategoryCommand { get; }

        public IDelegateCommand RemoveCategoryCommand { get; }

        public IDelegateCommand AddFeatureCommand { get; }

        public IDelegateCommand ShowCategoryFeaturesCommand { get; }

        public IDelegateCommand RemoveFeatureCommand { get; }

        #region DialogSettings

        public override int Width => 620;

        public override int MinWidth => 500;

        public override int MaxWidth => 800;

        public override int Height => 500;

        public override int MinHeight => 350;

        public override int MaxHeight => 700;

        #endregion

        public ReadOnlyObservableCollection<CategoryDto> AllCategories
        {
            get { return GetProperty(() => AllCategories); }
            private set { SetProperty(() => AllCategories, value); }
        }

        public AccessoryCategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public AccessoryFeatureViewItem SelectedFeature
        {
            get { return GetProperty(() => SelectedFeature); }
            set { SetProperty(() => SelectedFeature, value); }
        }

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Аксессуар";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override Task<Result<AccessoryDto>> CreateEntityAsync()
        {
            AccessorySaveDto saveDto = new AccessorySaveDto(
                Model.CategoryId.Value,
                Model.Active,
                Model.Categories.Select(x => new AccessoryCategorySaveDto(
                        x.Id,
                        x.AccessoryId,
                        x.CategoryId,
                        x.Features.Select(z => new AccessoryCategoryFeatureSaveDto(
                            z.Id,
                            z.AccessoryCategoryId,
                            z.FeatureId,
                            z.FeatureValueId)).ToArray()))
                    .ToArray(),
                Model.Features.Select(x => new AccessoryFeatureSaveDto(
                    x.Id,
                    x.FeatureId,
                    x.FeatureValueId)).ToArray());

            return WebClient.ExecuteApiRequestAsync(new CreateAccessory(saveDto));
        }

        protected override async Task HandleLoadedAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            AllCategories = categories.OrderBy(x => x.Left).ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();
        }

        protected override object CreateEntityMessage(AccessoryDto dto, MessageType messageType)
        {
            return new AccessoryMessage(dto, messageType);
        }

        protected override Task<AccessoryDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryAccessory(id));
        }

        protected override Task<LockResponse<AccessoryDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockAccessory(id));
        }

        protected override Task<LockResponse<AccessoryDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockAccessory(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание правила";
        }

        protected override void SetEditTitle()
        {
            Title = $"Правило отображения аксессуаров №{Model.Id}";
        }

        protected override void SetCopyTitle(int id)
        {
            Title = $"Копирование правила отображения аксессуаров №{id}";
        }

        protected override Task<Result<AccessoryDto>> UpdateEntityAsync()
        {
            AccessorySaveDto saveDto = new AccessorySaveDto(
                Model.Id,
                Model.CategoryId.Value,
                Model.Active,
                Model.Categories.Select(x => new AccessoryCategorySaveDto(
                    x.Id,
                    x.AccessoryId,
                    x.CategoryId,
                    x.Features.Select(z => new AccessoryCategoryFeatureSaveDto(
                        z.Id,
                        z.AccessoryCategoryId,
                        z.FeatureId,
                        z.FeatureValueId)).ToArray()))
                    .ToArray(),
                Model.Features.Select(x => new AccessoryFeatureSaveDto(
                    x.Id,
                    x.FeatureId,
                    x.FeatureValueId)).ToArray());

            return WebClient.ExecuteApiRequestAsync(new UpdateAccessory(saveDto));
        }

        protected override bool CanEdit()
        {
            return base.CanEdit() && WebClient.IsOperationAllowed(BusinessOperation.AccessoryUpdate);
        }

        private void AddCategory()
        {
            ChooseCategoryViewModel result = DialogDocumentManagerService.ShowView<ChooseCategoryViewModel>(new ChooseCategoryParameter(false, null), this);

            if (result.IsOk && result.SelectedCategory != null)
            {
                if (Model.Categories.Select(x => x.CategoryId).Contains(result.SelectedCategory.Id))
                {
                    MessageFacadeService.ShowNotificationWarning("Такая категория уже была добавлена");
                }
                else
                {
                    Model.Categories.Add(new AccessoryCategoryViewItem(result.SelectedCategory.Id, result.SelectedCategory.Name));
                }
            }
        }

        private void RemoveCategory()
        {
            Model.Categories.Remove(SelectedCategory);
        }

        private void ShowCategoryFeatures()
        {
            CategoryDto category = AllCategories.First(x => x.Id == SelectedCategory.CategoryId);

            if (category.Active != 1)
            {
                MessageFacadeService.ShowNotificationWarning("Категория не активна");
                return;
            }

            AccessoryCategoryFeatureViewModel viewModel = DialogDocumentManagerService.ShowView<AccessoryCategoryFeatureViewModel>(
                new AccessoryCategoryFeatureParameter(
                SelectedCategory.CategoryId,
                SelectedCategory.CategoryName,
                SelectedCategory.Id,
                SelectedCategory.Features),
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            SelectedCategory.Features = viewModel.Features;
        }

        private void AddFeature()
        {
            CategoryDto category = AllCategories.First(x => x.Id == Model.CategoryId.Value);

            if (category.Active != 1)
            {
                MessageFacadeService.ShowNotificationWarning("Категория не активна");
                return;
            }

            GetCategoryFeatureValueViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureValueViewModel>(new GetCategoryFeatureValueParameter(true, false, Model.CategoryId.Value, true), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (Model.Features.Any(x => x.FeatureId == viewModel.SelectedFeature.Value.Id && x.FeatureValueId == viewModel.SelectedFeatureValue.Value.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Такая характеристика уже добавлена");
                return;
            }

            Model.Features.Add(new AccessoryFeatureViewItem(
                0,
                viewModel.SelectedFeature.Value.Id,
                viewModel.SelectedFeatureValue.Value.Id,
                Model.Id,
                viewModel.SelectedFeature.Value.DisplayValue,
                viewModel.SelectedFeatureValue.Value.DisplayValue));
        }

        private void RemoveFeature()
        {
            Model.Features.Remove(SelectedFeature);
        }
    }
}