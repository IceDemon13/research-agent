using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Segment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Segment;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Segment
{
    public sealed class SegmentCategorySettingsViewModel : TelemartDialogViewModelBase
    {
        private TelemartEnumerableCompareHelper<SegmentCategorySettingsViewItem> compareHelper;

        public SegmentCategorySettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            SaveCommand = new AsyncCommand(SaveAsync);
            AddFeatureCommand = new DelegateCommand(AddFeature, () => SelectedCategory?.AllowEdit == true);
            DeleteAllFeaturesCommand = new DelegateCommand(DeleteAllFeatures, () => SelectedCategory?.Features?.Any() == true && SelectedCategory?.AllowEdit == true);
        }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand AddFeatureCommand { get; }

        public IDelegateCommand DeleteAllFeaturesCommand { get; }

        public ObservableCollection<SegmentCategorySettingsViewItem> CategorySettings
        {
            get { return GetProperty(() => CategorySettings); }
            private set { SetProperty(() => CategorySettings, value); }
        }

        public ReadOnlyObservableCollection<CategoryDto> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public SegmentCategorySettingsViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        #region DialogSettings

        public override int Width => 1000;

        public override int MinWidth => 700;

        public override int MaxWidth => 1400;

        public override int Height => 600;

        public override int MinHeight => 400;

        public override int MaxHeight => 800;

        #endregion

        private IErrorHandler ErrorHandler { get; }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Employees = employees.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            List<SegmentCategorySettingsDto> categorySettings = await WebClient.ExecuteApiRequestAsync(new QuerySegmentCategorySettings());

            bool allowEditAllOperation = WebClient.IsOperationAllowed(BusinessOperation.AllowEditAllSegmentCategoryFeatures);

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data.ToReadOnlyObservableCollection();

            CategorySettings = categorySettings
                .GroupBy(x => new { x.CategoryId, x.ParentCategoryId, x.CategoryName, x.CategoryEmployeeId, x.FilledInSegments })
                .Select(x => new SegmentCategorySettingsViewItem(
                    x.Key.CategoryId,
                    x.Key.ParentCategoryId,
                    x.Key.CategoryName,
                    x.Key.CategoryEmployeeId,
                    allowEditAllOperation || WebClient.AuthenticatedEmployee.Id == x.Key.CategoryEmployeeId,
                    x.Key.FilledInSegments,
                    x.Select(z => new SegmentCategorySettingsFeatureViewItem(z.Id, z.CategoryId, z.FeatureId, z.FeatureName))
                        .ToObservableCollection()))
                .ToObservableCollection();

            int[] currentParentCategories = categorySettings
                .GroupBy(x => x.CategoryId)
                .Select(x => x.Key)
                .ToArray();

            IEnumerable<SegmentCategorySettingsViewItem> newParentCategories = categories.Data
                .Where(x => x.Active == 1 && x.IsParent && !currentParentCategories.Contains(x.Id))
                .Select(x => new SegmentCategorySettingsViewItem(
                    x.Id,
                    x.ParentId,
                    x.Name,
                    x.EmployeeId,
                    allowEditAllOperation || WebClient.AuthenticatedEmployee.Id == x.EmployeeId,
                    false,
                    new ObservableCollection<SegmentCategorySettingsFeatureViewItem>()));

            CategorySettings.AddRange(newParentCategories);

            compareHelper = new TelemartEnumerableCompareHelper<SegmentCategorySettingsViewItem>(CategorySettings);

            await base.HandleLoadedAsync();

            Title = "Настройка сегментообразующих характеристик по категориям 🔧 📂";
        }

        protected override async Task HandleOkAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            Result<List<SegmentCategorySettingsDto>> result = await SaveAsync();

            result.IfNotNull(_ => CloseOk());
        }

        private void AddFeature()
        {
            GetCategoryFeatureViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureViewModel>(new GetCategoryFeatureParameter(true, false, SelectedCategory.CategoryId, false), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (SelectedCategory.DisplayFeatures?.Cast<SegmentCategorySettingsFeatureViewItem>().Select(x => x.FeatureId).Contains(viewModel.SelectedFeature.Value.Id) == true)
            {
                MessageFacadeService.ShowNotificationWarning("Характеристика уже добавлена");
                return;
            }

            SelectedCategory.Features ??= new ObservableCollection<SegmentCategorySettingsFeatureViewItem>();
            SelectedCategory.DisplayFeatures ??= new List<object>();

            SegmentCategorySettingsFeatureViewItem feature = new(0, SelectedCategory.CategoryId, viewModel.SelectedFeature.Value.Id, viewModel.SelectedFeature.Value.DisplayValue);

            SelectedCategory.Features.Add(feature);
            SelectedCategory.DisplayFeatures.Add(feature);

            SelectedCategory.DisplayFeatures = new List<object>(SelectedCategory.DisplayFeatures);

            RaisePropertiesChanged(nameof(SegmentCategorySettingsViewItem.DisplayFeatures), nameof(SegmentCategorySettingsViewItem.Features));
        }

        private void DeleteAllFeatures()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            SelectedCategory.Features.Clear();
            SelectedCategory.DisplayFeatures.Clear();
        }

        private async Task<Result<List<SegmentCategorySettingsDto>>> SaveAsync()
        {
            if (!compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return null;
            }

            CategorySettings.ForEach(x => x.DisplayFeatures ??= new List<object>());

            ICollection<SegmentCategorySettingsUpdateDto> updateDtos = CategorySettings
                .Where(x => x.AllowEdit)
                .SelectMany(x => x.DisplayFeatures)
                .Select(x => (SegmentCategorySettingsFeatureViewItem)x)
                .Select(x => new SegmentCategorySettingsUpdateDto(x.Id, x.CategoryId, x.FeatureId))
                .ToArray();

            Result<List<SegmentCategorySettingsDto>> result = await ErrorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new UpdateSegmentCategorySettings(updateDtos)), "сохранении характеристик", "Характеристики сохранены", this, true);

            result.IfNotNull(_ =>
            {
                compareHelper.UpdateObject(CategorySettings);
            });

            return result;
        }
    }
}