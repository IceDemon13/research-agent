using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Catalog;
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
    public sealed class AutoShowcasesHistoryViewModel : TelemartDialogViewModelBase
    {
        public AutoShowcasesHistoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messagefacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messagefacadeService)
        {
            ErrorHandler = errorHandler;
            AutoShowcasesHistories = new ObservableCollection<AutoShowcasesHistoryViewItem>();
        }

        public ObservableCollection<AutoShowcasesHistoryViewItem> AutoShowcasesHistories
        {
            get { return GetProperty(() => AutoShowcasesHistories); }
            private set { SetProperty(() => AutoShowcasesHistories, value); }
        }

        public AutoShowcasesHistoryViewItem AutoShowcasesHistory
        {
            get { return GetProperty(() => AutoShowcasesHistory); }
            set { SetProperty(() => AutoShowcasesHistory, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
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
            AutoShowcasesHistoryParameter parameter = (AutoShowcasesHistoryParameter)Parameter;

            await Task.WhenAll(
                RefreshWarehousesAsync(),
                RefreshCategoriesAsync(),
                RefreshEmployeesAsync(),
                LoadHistoriesByProductAsync(parameter.ProductId),
                RefreshProductAsync(parameter.ProductId));

            Title = $"История изменения плана по товару {ProductName}";

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private static AutoShowcasesHistoryViewItem MapToViewItem(
            ShowcaseHistoryDto history,
            int? categoryId)
        {
            return new AutoShowcasesHistoryViewItem()
            {
                ShowcaseHistoryId = history.ShowcaseHistoryId,
                WarehouseId = history.WarehouseId,
                ProductId = history.ProductId,
                CategoryId = categoryId ?? 0,
                CapacityNew = history.CapacityNew,
                ModifiedOn = history.ModifiedOn,
                ModifiedBy = history.ModifiedBy
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

        private async Task RefreshProductAsync(int productId)
        {
            List<ProductDto> products = await WebClient.ExecuteCatalogApiRequestAsync(new QueryProductByIds(new QueryProductByIdsDto(new[] { productId }, Constants.TelemartContractorId)));

            if (products?.Any() == true)
            {
                ProductName = products.Single().GetLocalName(LocalizableNameType.Ukr);
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            AllEmployees = employees
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadHistoriesByProductAsync(int productId)
        {
            Result<ShowcaseHistoriesDto> historiesResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryShowcaseHistories(productId)),
                "при загрузке истории",
                null,
                this,
                true,
                false);

            if (historiesResult.IsSuccess && historiesResult.Data?.ShowcaseHistories?.Any() == true)
            {
                AutoShowcasesHistories = historiesResult.Data?.ShowcaseHistories
                    .Select(x => MapToViewItem(x, historiesResult.Data.CategoryId))
                    .ToObservableCollection();
            }
        }
    }
}