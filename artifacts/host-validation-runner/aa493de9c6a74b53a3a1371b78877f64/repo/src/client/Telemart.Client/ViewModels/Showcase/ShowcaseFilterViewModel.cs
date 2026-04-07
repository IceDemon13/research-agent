using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Directories.Category;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<WarehouseDto> _warehousesList;
        private List<EmployeeDto> _employees;
        private List<CategoryDto> _categories;

        public ShowcaseFilterViewModel(IWebClient webClient, IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public ShowcaseFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableCollection<CategoryViewItem> Categories { get; } = new ObservableCollection<CategoryViewItem>();

        #endregion

        public ObservableCollection<int> WarehouseIds
        {
            get { return GetProperty(() => WarehouseIds); }
            set { SetProperty(() => WarehouseIds, value); }
        }

        public ComboBoxItem? Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
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

        public DateTime? From
        {
            get { return GetProperty(() => From); }
            set { SetProperty(() => From, value); }
        }

        public DateTime? To
        {
            get { return GetProperty(() => To); }
            set { SetProperty(() => To, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public IFilteringItem GetFilteringItem()
        {
            IFilteringItem item = new ShowcaseFilteringItem(Product?.Id, WarehouseIds?.ToArray(), SelectedCategoryEmployeeId, SelectedCategoryId, From, To);

            return item;
        }

        public async Task RefreshAsync()
        {
            await Task.WhenAll(RefreshWarehousesAsync(), RefreshEmployeesAsync(), RefreshCategoriesAsync());

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
            From = DateTime.Today.AddMonths(-3);
            To = DateTime.Now;
            WarehouseIds = null;
            Product = null;
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

            _warehousesList = warehouses;

            List<ComboBoxItem> employeeItems = _warehousesList
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.ShowCase.Id || x.TypeId == WarehouseKind.Pickup.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToList();

            Warehouses.AddRange(employeeItems);
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