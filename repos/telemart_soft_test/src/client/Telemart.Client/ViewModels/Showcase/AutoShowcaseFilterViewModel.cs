using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Locations;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Showcase
{
    public class AutoShowcaseFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<WarehouseDto> _warehousesList;
        private List<EmployeeDto> _employees;
        private List<CategoryDto> _categories;
        private List<ClusterDto> _clusters;
        private bool allWarehousesAccess;

        public AutoShowcaseFilterViewModel(IWebClient webClient, IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public AutoShowcaseFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableCollection<CategoryViewItem> Categories { get; } = new ObservableCollection<CategoryViewItem>();

        public ObservableCollection<ComboBoxItem> Clusters { get; } = new ObservableCollection<ComboBoxItem>();

        #endregion

        public ObservableCollection<int> WarehouseIds
        {
            get { return GetProperty(() => WarehouseIds); }
            set { SetProperty(() => WarehouseIds, value); }
        }

        public ObservableCollection<int> ClusterIds
        {
            get { return GetProperty(() => ClusterIds); }
            set { SetProperty(() => ClusterIds, value, () => RaisePropertyChanged(nameof(WarehouseIds))); }
        }

        public int? SelectedCategoryEmployeeId
        {
            get { return GetProperty(() => SelectedCategoryEmployeeId); }
            set { SetProperty(() => SelectedCategoryEmployeeId, value); }
        }

        public int? SelectedCategoryId
        {
            get { return GetProperty(() => SelectedCategoryId); }
            set { SetProperty(() => SelectedCategoryId, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<AutoShowcaseFilterViewModel> builder)
        {
            builder.Property(x => x.WarehouseIds)
                .MatchesInstanceRule((x, y) => x?.Any() == true, () => "Должен быть выбран минимум 1 склад");
        }

        public IFilteringItem GetFilteringItem()
        {
            IFilteringItem item = new AutoShowcaseFilteringItem(WarehouseIds?.ToArray(), ClusterIds?.ToArray(), SelectedCategoryEmployeeId, SelectedCategoryId);

            return item;
        }

        public async Task RefreshAsync()
        {
            await Task.WhenAll(RefreshWarehousesAsync(), RefreshEmployeesAsync(), RefreshCategoriesAsync(), RefreshClustersAsync());

            int[] categoryEmployeeIds = _categories?.Select(x => x.EmployeeId).ToArray();

            Employees.Clear();

            Employees.AddRange(
                _employees.Where(
                        x => x.Active && (categoryEmployeeIds?.Contains(x.Id) == true || x.Roles?.Contains(Role.Product.Name) == true || x.Id == WebClient.AuthenticatedEmployee.Id))
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .OrderBy(x => x.DisplayValue));
        }

        public void ResetFilterValues()
        {
            WarehouseIds = Warehouses.Select(x => x.Id).ToObservableRangeCollection();
            ClusterIds = null;
            SelectedCategoryId = null;
            SelectedCategoryEmployeeId = WebClient.AuthenticatedEmployee.HasAnyRole(Role.Product) ? WebClient.AuthenticatedEmployee.Id : null;
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, _warehousesList))
            {
                return;
            }

            Warehouses.Clear();

            allWarehousesAccess = WebClient.IsOperationAllowed(BusinessOperation.ShowcaseAllWarehousesAccess);

            _warehousesList = warehouses;

            List<ComboBoxItem> warehouseItems = _warehousesList
                .OrderBy(x => x.CityId)
                .ThenBy(x => x.Name)
                .Where(x =>
                    x.Active == 1
                    && (x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Pickup.Id)
                    && (allWarehousesAccess || WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id)))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToList();

            Warehouses.AddRange(warehouseItems);
        }

        private async Task RefreshClustersAsync()
        {
            IReadOnlyCollection<ClusterDto> clusters = await WebClient.ExecuteApiRequestAsync(new QueryClusters());

            if (ReferenceEquals(clusters, _clusters))
            {
                return;
            }

            _clusters = clusters.ToList();

            Clusters.Clear();

            Clusters.AddRange(clusters.Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, _employees))
            {
                return;
            }

            _employees = employees;
        }

        private async Task RefreshCategoriesAsync()
        {
            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(categories, _categories))
            {
                return;
            }

            Categories.Clear();

            Categories.AddRange(
                categories
                    .Where(x => x.Active > 0 && x.IsParent)
                    .OrderBy(x => x.Name)
                    .Select(x => Mapper.Map<CategoryViewItem>(x)));

            _categories = categories;
        }
    }
}