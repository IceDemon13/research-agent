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
using Telemart.Client.Data.Requests.Features.TradeInSegment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public sealed class TradeInSegmentCategorySettingsViewModel : TelemartDialogViewModelBase
    {
        private TelemartEnumerableCompareHelper<TradeInSegmentCategorySettingsViewItem> _compareHelper;

        public TradeInSegmentCategorySettingsViewModel(
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

        public ObservableCollection<TradeInSegmentCategorySettingsViewItem> CategorySettings
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

        public TradeInSegmentCategorySettingsViewItem SelectedCategory
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
            if (!IsOk && _compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
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

            List<TradeInSegmentCategorySettingsDto> categorySettings = await WebClient.ExecuteApiRequestAsync(new QueryTradeInSegmentCategorySettings());

            bool allowEditAllOperation = WebClient.IsOperationAllowed(BusinessOperation.AllowEditAllTradeInSegmentCategoryFeatures);

            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            Categories = categories.Data.ToReadOnlyObservableCollection();

            CategorySettings = categorySettings
                .GroupBy(x => new { x.CategoryId, x.ParentCategoryId, x.CategoryName, x.CategoryEmployeeId, x.FilledInSegments })
                .Select(x => new TradeInSegmentCategorySettingsViewItem(
                    x.Key.CategoryId,
                    x.Key.ParentCategoryId,
                    x.Key.CategoryName,
                    x.Key.CategoryEmployeeId,
                    allowEditAllOperation || WebClient.AuthenticatedEmployee.Id == x.Key.CategoryEmployeeId,
                    x.Key.FilledInSegments,
                    x.Select(z => new TradeInSegmentCategorySettingsFeatureViewItem(z.Id, z.CategoryId, z.FeatureId, z.FeatureName, z.ParentCategoryId, z.CategoryName, z.CategoryEmployeeId, z.FilledInSegments))
                        .ToObservableCollection()))
                .ToObservableCollection();

            int[] currentParentCategories = categorySettings
                .GroupBy(x => x.CategoryId)
                .Select(x => x.Key)
                .ToArray();

            IEnumerable<TradeInSegmentCategorySettingsViewItem> newParentCategories = categories.Data
                .Where(x => x.Active == 1 && x.IsParent && !currentParentCategories.Contains(x.Id))
                .Select(x => new TradeInSegmentCategorySettingsViewItem(
                    x.Id,
                    x.ParentId,
                    x.Name,
                    x.EmployeeId,
                    allowEditAllOperation || WebClient.AuthenticatedEmployee.Id == x.EmployeeId,
                    false,
                    new ObservableCollection<TradeInSegmentCategorySettingsFeatureViewItem>()));

            CategorySettings.AddRange(newParentCategories);

            _compareHelper = new TelemartEnumerableCompareHelper<TradeInSegmentCategorySettingsViewItem>(CategorySettings);

            await base.HandleLoadedAsync();

            Title = "Настройка сегментообразующих характеристик по категориям Trade-In 🔧 📂";
        }

        protected override async Task HandleOkAsync()
        {
            if (!_compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            Result<List<TradeInSegmentCategorySettingsDto>> result = await SaveAsync();

            result.IfNotNull(_ => CloseOk());
        }

        private void AddFeature()
        {
            GetCategoryFeatureViewModel viewModel = DialogDocumentManagerService.ShowView<GetCategoryFeatureViewModel>(new GetCategoryFeatureParameter(true, false, SelectedCategory.CategoryId, false), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (SelectedCategory.DisplayFeatures?.Cast<TradeInSegmentCategorySettingsFeatureViewItem>().Select(x => x.FeatureId).Contains(viewModel.SelectedFeature.Value.Id) == true)
            {
                MessageFacadeService.ShowNotificationWarning("Характеристика уже добавлена");
                return;
            }

            SelectedCategory.Features ??= new ObservableCollection<TradeInSegmentCategorySettingsFeatureViewItem>();
            SelectedCategory.DisplayFeatures ??= new List<object>();

            TradeInSegmentCategorySettingsFeatureViewItem feature = new(
                0,
                SelectedCategory.CategoryId,
                viewModel.SelectedFeature.Value.Id,
                viewModel.SelectedFeature.Value.DisplayValue,
                SelectedCategory.ParentCategoryId,
                SelectedCategory.CategoryName,
                SelectedCategory.CategoryEmployeeId,
                SelectedCategory.FilledInSegments);

            SelectedCategory.Features.Add(feature);
            SelectedCategory.DisplayFeatures.Add(feature);

            SelectedCategory.DisplayFeatures = new List<object>(SelectedCategory.DisplayFeatures);

            RaisePropertiesChanged(nameof(TradeInSegmentCategorySettingsViewItem.DisplayFeatures), nameof(TradeInSegmentCategorySettingsViewItem.Features));
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

        private async Task<Result<List<TradeInSegmentCategorySettingsDto>>> SaveAsync()
        {
            if (!_compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return null;
            }

            CategorySettings.ForEach(x => x.DisplayFeatures ??= new List<object>());

            ICollection<TradeInSegmentCategorySettingsUpdateDto> updateDtos = CategorySettings
                .Where(x => x.AllowEdit)
                .SelectMany(x => x.DisplayFeatures)
                .Select(x => (TradeInSegmentCategorySettingsFeatureViewItem)x)
                .Select(x => new TradeInSegmentCategorySettingsUpdateDto(x.Id, x.CategoryId, x.FeatureId))
                .ToArray();

            Result<List<TradeInSegmentCategorySettingsDto>> result = await ErrorHandler.HandleErrorsAsync(ct => WebClient.ExecuteApiRequestAsync(new UpdateTradeInSegmentCategorySettings(updateDtos)), "сохранении характеристик", "Характеристики сохранены", this, true);

            result.IfNotNull(_ =>
            {
                _compareHelper.UpdateObject(CategorySettings);
            });

            return result;
        }
    }
}