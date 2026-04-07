using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.OrderSource;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class StoreOrdersFilterViewModel : BindableBase, IDataErrorInfo, IFilteringViewModel<OrderFilteringItem>
    {
        private readonly ComboBoxItem notSetResponsiblePerson;

        private List<CityDto> citiesList;
        private List<ContractorDto> contractorsList;
        private List<EmployeeDto> employeesList;
        private List<WarehouseDto> warehousesList;
        private List<LegalEntityDto> legalEntities;
        private List<OrderSourceDto> orderSources;

        public StoreOrdersFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            Subdivisions.AddRange(Dictionaries.GetItems<Subdivision>().Where(x => x.Active));
            Payments.AddRange(Dictionaries.GetItems<Payment>().Where(x => x.Active && x.Id != Payment.TerminalId));
            Carries.AddRange(Dictionaries.GetItems<CarryType>().Where(x => x.IsActive()));
            Statuses.AddRange(Dictionaries.GetItems<OrderStatus>());

            notSetResponsiblePerson = new ComboBoxItem(0, Constants.EmptyFilterItemDisplayValue);
        }

        public StoreOrdersFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<CarryType> Carries { get; } = new ObservableRangeCollection<CarryType>();

        public ObservableRangeCollection<ComboBoxItem> Cities { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> Contractors { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> LegalEntities { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> OrderSources { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> ResponsibleEmployees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<Payment> Payments { get; } = new ObservableRangeCollection<Payment>();

        public ObservableRangeCollection<OrderStatus> Statuses { get; } = new ObservableRangeCollection<OrderStatus>();

        public ObservableRangeCollection<Subdivision> Subdivisions { get; } = new ObservableRangeCollection<Subdivision>();

        public ObservableRangeCollection<ComboBoxItem> Warehouses { get; } = new ObservableRangeCollection<ComboBoxItem>();

        #endregion

        public string Cellphone
        {
            get { return GetProperty(() => Cellphone); }
            set { SetProperty(() => Cellphone, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string TrackNumbers
        {
            get { return GetProperty(() => TrackNumbers); }
            set { SetProperty(() => TrackNumbers, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public DateTime? OrderClosedAfter
        {
            get { return GetProperty(() => OrderClosedAfter); }
            set { SetProperty(() => OrderClosedAfter, value); }
        }

        public DateTime? OrderClosedBefore
        {
            get { return GetProperty(() => OrderClosedBefore); }
            set { SetProperty(() => OrderClosedBefore, value); }
        }

        public DateTime? OrderCreatedAfter
        {
            get { return GetProperty(() => OrderCreatedAfter); }
            set { SetProperty(() => OrderCreatedAfter, value); }
        }

        public DateTime? OrderCreatedBefore
        {
            get { return GetProperty(() => OrderCreatedBefore); }
            set { SetProperty(() => OrderCreatedBefore, value); }
        }

        public DateTime? CompletedOnAfter
        {
            get { return GetProperty(() => CompletedOnAfter); }
            set { SetProperty(() => CompletedOnAfter, value); }
        }

        public DateTime? CompletedOnBefore
        {
            get { return GetProperty(() => CompletedOnBefore); }
            set { SetProperty(() => CompletedOnBefore, value); }
        }

        public string OrderNumbers
        {
            get { return GetProperty(() => OrderNumbers); }
            set { SetProperty(() => OrderNumbers, value); }
        }

        public string ExternalOrderNumbers
        {
            get { return GetProperty(() => ExternalOrderNumbers); }
            set { SetProperty(() => ExternalOrderNumbers, value); }
        }

        public string ServiceRequestNumbers
        {
            get { return GetProperty(() => ServiceRequestNumbers); }
            set { SetProperty(() => ServiceRequestNumbers, value); }
        }

        public bool? PkoBool
        {
            get { return GetProperty(() => PkoBool); }
            set { SetProperty(() => PkoBool, value); }
        }

        public bool? CanceledFromSite
        {
            get { return GetProperty(() => CanceledFromSite); }
            set { SetProperty(() => CanceledFromSite, value); }
        }

        public bool? CompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => CompletedOnFiscalRegistrar); }
            set { SetProperty(() => CompletedOnFiscalRegistrar, value); }
        }

        public bool? AnyNewCalls
        {
            get { return GetProperty(() => AnyNewCalls); }
            set { SetProperty(() => AnyNewCalls, value); }
        }

        public bool? ChangeOrder
        {
            get { return GetProperty(() => ChangeOrder); }
            set { SetProperty(() => ChangeOrder, value); }
        }

        public bool? ReceivedByCustomer
        {
            get { return GetProperty(() => ReceivedByCustomer); }
            set { SetProperty(() => ReceivedByCustomer, value); }
        }

        public string Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public bool? RtBool
        {
            get { return GetProperty(() => RtBool); }
            set { SetProperty(() => RtBool, value); }
        }

        public List<object> SelectedCities
        {
            get { return GetProperty(() => SelectedCities); }
            set { SetProperty(() => SelectedCities, value); }
        }

        public List<object> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public List<object> SelectedLegalEntities
        {
            get { return GetProperty(() => SelectedLegalEntities); }
            set { SetProperty(() => SelectedLegalEntities, value); }
        }

        public List<object> SelectedOrderSources
        {
            get { return GetProperty(() => SelectedOrderSources); }
            set { SetProperty(() => SelectedOrderSources, value); }
        }

        public List<object> SelectedManagers
        {
            get { return GetProperty(() => SelectedManagers); }
            set { SetProperty(() => SelectedManagers, value); }
        }

        public List<object> SelectedResponsibleEmployees
        {
            get { return GetProperty(() => SelectedResponsibleEmployees); }
            set { SetProperty(() => SelectedResponsibleEmployees, value); }
        }

        public List<object> SelectedCreatedByEmployees
        {
            get { return GetProperty(() => SelectedCreatedByEmployees); }
            set { SetProperty(() => SelectedCreatedByEmployees, value); }
        }

        public List<object> SelectedWarehouses
        {
            get { return GetProperty(() => SelectedWarehouses); }
            set { SetProperty(() => SelectedWarehouses, value); }
        }

        public ObservableCollection<Payment> SelectedPayments
        {
            get { return GetProperty(() => SelectedPayments); }
            set { SetProperty(() => SelectedPayments, value); }
        }

        public ObservableCollection<OrderStatus> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        public ObservableCollection<Subdivision> SelectedSubdivisions
        {
            get { return GetProperty(() => SelectedSubdivisions); }
            set { SetProperty(() => SelectedSubdivisions, value); }
        }

        public ObservableCollection<CarryType> SelectedCarries
        {
            get { return GetProperty(() => SelectedCarries); }
            set { SetProperty(() => SelectedCarries, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<StoreOrdersFilterViewModel> builder)
        {
            builder.Property(x => x.OrderNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.ServiceRequestNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public OrderFilteringItem GetFilteringItem()
        {
            OrderFilteringItem item = new OrderFilteringItem(Fio, SelectedSubdivisions.Select(x => x.Id).ToList())
            {
                OrderNumbers = OrderNumbers,
                ExternalOrderNumbers = ExternalOrderNumbers,
                ServiceRequestNumbers = ServiceRequestNumbers,
                Cellphone = Cellphone,
                Email = Email,
                TrackNumbers = TrackNumbers,
                OrderCreatedAfter = OrderCreatedAfter,
                OrderCreatedBefore = OrderCreatedBefore,
                OrderClosedAfter = OrderClosedAfter,
                OrderClosedBefore = OrderClosedBefore,
                PkoBool = PkoBool,
                RtBool = RtBool,
                ChangeOrder = ChangeOrder,
                AnyNewCalls = AnyNewCalls,
                ReceivedByCustomer = ReceivedByCustomer,
                Payments = SelectedPayments.Select(x => x.Id).ToList(),
                Cities = SelectedCities?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                Contractors = SelectedContractors?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                LegalEntities = SelectedLegalEntities?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                OrderSources = SelectedOrderSources?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                Warehouses = SelectedWarehouses?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                Carries = SelectedCarries.Select(x => x.Id).ToList(),
                OrderStatuses = SelectedStatuses.Select(x => x.Id).ToList(),
                Managers = SelectedManagers?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                ResponsibleEmployees = SelectedResponsibleEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                CreatedByEmployees = SelectedCreatedByEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                Product = Product,
                IncludeCustomerStateText = true,
                IncludePreorders = true,
                CanceledFromSite = CanceledFromSite,
                OrderCompletedOnAfter = CompletedOnAfter,
                OrderCompletedOnBefore = CompletedOnBefore,
                CompletedOnFiscalRegistrar = CompletedOnFiscalRegistrar
            };

            return item;
        }

        public void SetFilteringItem(OrderFilteringItem fi)
        {
            OrderNumbers = fi.OrderNumbers;
            ExternalOrderNumbers = fi.ExternalOrderNumbers;
            Fio = fi.Fio;
            TrackNumbers = fi.TrackNumbers;
            RtBool = fi.RtBool;
            PkoBool = fi.PkoBool;
            Cellphone = fi.Cellphone;
            Email = fi.Email;
            AnyNewCalls = fi.AnyNewCalls;
            Product = fi.Product;
            OrderCreatedAfter = fi.OrderCreatedAfter;
            OrderCreatedBefore = fi.OrderCreatedBefore;
            OrderClosedAfter = fi.OrderClosedAfter;
            OrderClosedBefore = fi.OrderClosedBefore;
            ChangeOrder = fi.ChangeOrder;
            ReceivedByCustomer = fi.ReceivedByCustomer;
            CompletedOnFiscalRegistrar = fi.CompletedOnFiscalRegistrar;
            CanceledFromSite = fi.CanceledFromSite;
            SelectedPayments = fi.Payments?.Select(x => Payments.First(z => z.Id == x)).ToObservableCollection() ?? new ObservableCollection<Payment>();

            SelectedCities = fi.Cities?.Select(x =>
            {
                CityDto city = citiesList.First(z => z.Id == x);
                return new ComboBoxItem(city.Id, city.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedStatuses = fi.OrderStatuses?.Select(x => Statuses.First(z => z.Id == x)).ToObservableCollection() ?? new ObservableCollection<OrderStatus>();

            SelectedWarehouses = fi.Warehouses?.Select(x =>
            {
                WarehouseDto warehouse = warehousesList.First(z => z.Id == x);
                return new ComboBoxItem(warehouse.Id, warehouse.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedResponsibleEmployees = fi.ResponsibleEmployees?.Select(x =>
            {
                EmployeeDto employee = employeesList.First(z => z.Id == x);
                return new ComboBoxItem(employee.Id, employee.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedManagers = fi.Managers?.Select(x =>
            {
                EmployeeDto employee = employeesList.First(z => z.Id == x);
                return new ComboBoxItem(employee.Id, employee.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedCarries = fi.Carries?.Select(x => Carries.First(z => z.Id == x)).ToObservableCollection() ?? new ObservableCollection<CarryType>();

            SelectedContractors = fi.Contractors?.Select(x =>
            {
                ContractorDto contractor = contractorsList.First(z => z.Id == x);
                return new ComboBoxItem(contractor.Id, contractor.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedLegalEntities = fi.LegalEntities?.Select(x =>
            {
                LegalEntityDto legalEntityDto = legalEntities.First(z => z.Id == x);
                return new ComboBoxItem(legalEntityDto.Id, legalEntityDto.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedOrderSources = fi.OrderSources?.Select(x =>
            {
                OrderSourceDto orderSourceDto = orderSources.First(z => z.Id == x);
                return new ComboBoxItem(orderSourceDto.Id, orderSourceDto.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            SelectedCreatedByEmployees = fi.CreatedByEmployees?.Select(x =>
            {
                EmployeeDto employee = employeesList.First(z => z.Id == x);
                return new ComboBoxItem(employee.Id, employee.Name);
            }).Cast<object>().ToList() ?? new List<object>();

            ServiceRequestNumbers = fi.ServiceRequestNumbers;

            CompletedOnAfter = fi.OrderCompletedOnAfter;
            CompletedOnBefore = fi.OrderCompletedOnBefore;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(
                RefreshCitiesAsync(),
                RefreshEmployeesAsync(),
                RefreshContractorsAsync(),
                RefreshWarehousesAsync(),
                RefreshLegalEntitiesAsync(),
                RefreshOrderSourcesAsync());
        }

        public void ResetFilterValues()
        {
            OrderCreatedAfter = null;
            OrderCreatedBefore = null;
            OrderClosedAfter = null;
            OrderClosedBefore = null;
            OrderNumbers = null;
            ExternalOrderNumbers = null;
            ServiceRequestNumbers = null;
            SelectedSubdivisions = new ObservableCollection<Subdivision>();
            SelectedContractors = new List<object>();
            SelectedLegalEntities = new List<object>();
            SelectedOrderSources = new List<object>();
            Fio = string.Empty;
            TrackNumbers = string.Empty;
            Cellphone = string.Empty;
            Email = string.Empty;
            SelectedCities = new List<object>();
            SelectedPayments = new ObservableCollection<Payment>();
            SelectedCarries = new ObservableCollection<CarryType>();
            SelectedWarehouses = warehousesList
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .Cast<object>()
                .ToList();
            SelectedStatuses = new ObservableCollection<OrderStatus> { OrderStatus.Received, OrderStatus.Confirmed, OrderStatus.Packed };
            SelectedManagers = new List<object>();
            SelectedResponsibleEmployees = new List<object>();
            SelectedCreatedByEmployees = new List<object>();
            PkoBool = null;
            RtBool = null;
            Product = null;
            ChangeOrder = null;
            ReceivedByCustomer = null;
            AnyNewCalls = null;
            CompletedOnAfter = null;
            CompletedOnBefore = null;
            CompletedOnFiscalRegistrar = null;
            CanceledFromSite = null;
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(cities, citiesList))
            {
                return;
            }

            Cities.Clear();
            citiesList = cities;
            Cities.AddRange(citiesList.OrderBy(x => x.Position).ThenBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            Contractors.Clear();
            contractorsList = contractors;
            Contractors.AddRange(contractorsList.Where(x => x.Active && x.IsFolder == false).OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)));
        }

        private async Task RefreshLegalEntitiesAsync()
        {
            List<LegalEntityDto> legalEntitieDtos = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

            if (ReferenceEquals(legalEntities, legalEntitieDtos))
            {
                return;
            }

            LegalEntities.Clear();

            legalEntities = legalEntitieDtos;

            LegalEntities.AddRange(legalEntitieDtos
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection());
        }

        private async Task RefreshOrderSourcesAsync()
        {
            List<OrderSourceDto> orderSourceDtos = await WebClient.ExecuteApiRequestAsync(new QueryOrderSources(), true);

            if (ReferenceEquals(orderSources, orderSourceDtos))
            {
                return;
            }

            OrderSources.Clear();

            orderSources = orderSourceDtos;

            OrderSources.AddRange(orderSourceDtos
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection());
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();
            ResponsibleEmployees.Clear();

            employeesList = employees;

            List<ComboBoxItem> employeeItems = employeesList.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Employees.AddRange(employeeItems);
            ResponsibleEmployees.Add(notSetResponsiblePerson);
            ResponsibleEmployees.AddRange(employeeItems);
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(warehouses, warehousesList))
            {
                return;
            }

            Warehouses.Clear();
            warehousesList = warehouses;

            Warehouses.AddRange(warehouses.Where(x => x.Active == 1).OrderByDescending(x => x.Position).Select(x => new ComboBoxItem(x.Id, x.Name)));
        }
    }
}