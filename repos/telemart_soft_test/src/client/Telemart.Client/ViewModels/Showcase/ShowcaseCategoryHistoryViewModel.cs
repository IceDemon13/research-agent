using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Showcase.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Debezium;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryHistoryViewModel : TelemartDialogViewModelBase
    {
        public ShowcaseCategoryHistoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messagefacadeService)
        {
            ErrorHandler = errorHandler;
        }

        public ObservableCollection<ShowcaseCategoryHistoryViewItem> ShowcaseCategoryHistories
        {
            get { return GetProperty(() => ShowcaseCategoryHistories); }
            private set { SetProperty(() => ShowcaseCategoryHistories, value); }
        }

        public ShowcaseCategoryHistoryViewItem ShowcaseCategoryHistory
        {
            get { return GetProperty(() => ShowcaseCategoryHistory); }
            set { SetProperty(() => ShowcaseCategoryHistory, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        #region DialogSettings

        public override int Width => 720;

        public override int MinWidth => 600;

        public override int MaxWidth => 1000;

        public override int Height => 500;

        public override int MinHeight => 350;

        public override int MaxHeight => 700;

        #endregion

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            ShowcaseCategoryHistoryParameter parameter = (ShowcaseCategoryHistoryParameter)Parameter;

            await Task.WhenAll(
                RefreshWarehousesAsync(),
                RefreshCategoriesAsync(),
                RefreshEmployeesAsync(),
                LoadHistoriesByProductAsync(parameter));

            Title = "История изменения категории плана";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private static ShowcaseCategoryHistoryViewItem MapToViewItem(
            ShowcaseCategoryHistoryDto history)
        {
            return new ShowcaseCategoryHistoryViewItem()
            {
                Ref = history.Ref,
                WarehouseId = history.WarehouseId,
                CategoryId = history.CategoryId,
                CategoryPlan = history.Quantity,
                ModifiedOn = history.ModifiedOn,
                ModifiedBy = history.ModifiedBy,
                PlaceName = history.PlaceName
            };
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            AllEmployees = employees
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadHistoriesByProductAsync(ShowcaseCategoryHistoryParameter parameter)
        {
            Result<ShowcaseCategoryHistoriesDto> historiesResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryShowcaseCategoryHistories(parameter.From, parameter.To, parameter.WarehouseIds, parameter.CategoryId)),
                "при загрузке истории",
                null,
                this,
                true,
                false);

            if (historiesResult.IsSuccess && historiesResult.Data?.ShowcaseCategoryHistories?.Any() == true)
            {
                ShowcaseCategoryHistories = historiesResult.Data?.ShowcaseCategoryHistories
                    .Select(MapToViewItem)
                    .ToObservableCollection();
            }
        }
    }
}