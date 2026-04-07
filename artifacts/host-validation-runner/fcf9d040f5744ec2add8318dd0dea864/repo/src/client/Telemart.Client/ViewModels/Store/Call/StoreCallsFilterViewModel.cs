using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class StoreCallsFilterViewModel : ViewModelBase, IDataErrorInfo, IFilteringViewModel<CallFilteringItem>
    {
        private List<EmployeeDto> employeesList;
        private List<ContractorDto> contractorsList;
        private List<CallTypeDto> callTypesList;
        private bool isInitialized;

        public StoreCallsFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            Subdivisions.AddRange(Dictionaries.GetItems<Subdivision>().Where(x => x.Active && WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.Id)));
            CallStates.AddRange(Dictionaries.GetItems<CallState>().Where(x => x.Active));

            Available = true;
            Completed = true;

            InitializeObservableCollections();
        }

        public ObservableRangeCollection<Subdivision> Subdivisions { get; } = new ObservableRangeCollection<Subdivision>();

        public ObservableRangeCollection<ContractorDto> Contractors { get; } = new ObservableRangeCollection<ContractorDto>();

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<CallTypeDto> CallTypes { get; } = new ObservableRangeCollection<CallTypeDto>();

        public ObservableRangeCollection<CallState> CallStates { get; } = new ObservableRangeCollection<CallState>();

        #region INPC

        public string CallIds
        {
            get { return GetProperty(() => CallIds); }
            set { SetProperty(() => CallIds, value); }
        }

        public string OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public string ServiceRequestIds
        {
            get { return GetProperty(() => ServiceRequestIds); }
            set { SetProperty(() => ServiceRequestIds, value); }
        }

        public DateTime? CallCreatedFrom
        {
            get { return GetProperty(() => CallCreatedFrom); }
            set { SetProperty(() => CallCreatedFrom, value); }
        }

        public DateTime? CallCreatedTo
        {
            get { return GetProperty(() => CallCreatedTo); }
            set { SetProperty(() => CallCreatedTo, value); }
        }

        public DateTime? CallCompletedFrom
        {
            get { return GetProperty(() => CallCompletedFrom); }
            set { SetProperty(() => CallCompletedFrom, value); }
        }

        public DateTime? CallCompletedTo
        {
            get { return GetProperty(() => CallCompletedTo); }
            set { SetProperty(() => CallCompletedTo, value); }
        }

        public string PhoneNumbers
        {
            get { return GetProperty(() => PhoneNumbers); }
            set { SetProperty(() => PhoneNumbers, value); }
        }

        public ObservableCollection<Subdivision> SelectedSubdivisions
        {
            get { return GetProperty(() => SelectedSubdivisions); }
            set { SetProperty(() => SelectedSubdivisions, value); }
        }

        public CallTypeDto SelectedCallType
        {
            get { return GetProperty(() => SelectedCallType); }
            set { SetProperty(() => SelectedCallType, value); }
        }

        public ObservableCollection<CallState> SelectedCallStates
        {
            get { return GetProperty(() => SelectedCallStates); }
            set { SetProperty(() => SelectedCallStates, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public List<object> SelectedResponsiblePersons
        {
            get { return GetProperty(() => SelectedResponsiblePersons); }
            set { SetProperty(() => SelectedResponsiblePersons, value); }
        }

        public List<object> SelectedCompletedByEmployees
        {
            get { return GetProperty(() => SelectedCompletedByEmployees); }
            set { SetProperty(() => SelectedCompletedByEmployees, value); }
        }

        public ContractorDto SelectedContractor
        {
            get { return GetProperty(() => SelectedContractor); }
            set { SetProperty(() => SelectedContractor, value); }
        }

        public List<object> SelectedContractorManagers
        {
            get { return GetProperty(() => SelectedContractorManagers); }
            set { SetProperty(() => SelectedContractorManagers, value); }
        }

        public bool Available
        {
            get { return GetProperty(() => Available); }
            set { SetProperty(() => Available, value); }
        }

        public bool Completed
        {
            get { return GetProperty(() => Completed); }
            set { SetProperty(() => Completed, value); }
        }

        #endregion

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public Task RefreshAsync()
        {
            return Task.WhenAll(
                RefreshEmployeesAsync(),
                RefreshCallTypesAsync(),
                RefreshContractorsAsync());
        }

        public void ResetFilterValues()
        {
            CallIds = null;
            OrderIds = null;
            ServiceRequestIds = null;
            CallCreatedFrom = null;
            CallCreatedTo = null;
            CallCompletedFrom = null;
            CallCompletedTo = null;
            PhoneNumbers = null;
            SelectedSubdivisions = new ObservableCollection<Subdivision>();
            SelectedContractor = null;
            SelectedCallType = null;
            Fio = string.Empty;
            SelectedContractorManagers = new List<object>();
            SelectedResponsiblePersons = new List<object>();
            SelectedCompletedByEmployees = new List<object>();
            SelectedCallStates = new ObservableCollection<CallState>(GetDefaultStates());
            Available = true;
            Completed = true;
        }

        public CallFilteringItem GetCallFilteringItem()
        {
            CallFilteringItem item = new CallFilteringItem();

            if (!isInitialized)
            {
                SelectedCallType = null;
                isInitialized = true;
            }

            List<int> childCallTypes = null;
            if (SelectedCallType != null)
            {
                childCallTypes = callTypesList.Where(x => x.ParentId.HasValue && x.ParentId.Value == SelectedCallType.Id).Select(x => x.Id).ToList();
                childCallTypes.Add(SelectedCallType.Id);
            }

            item.CallTypes = childCallTypes;
            item.CallStates = SelectedCallStates.Select(x => x.Id).ToList();
            item.CallIds = CallIds;
            item.OrderIds = OrderIds;
            item.ServiceRequestIds = ServiceRequestIds;
            item.CallCreatedFrom = CallCreatedFrom;
            item.CallCreatedTo = CallCreatedTo;
            item.CallCompletedFrom = CallCompletedFrom;
            item.CallCompletedTo = CallCompletedTo;
            item.CompletedByEmployees = SelectedCompletedByEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToList();
            item.Fio = Fio;
            item.PhoneNumbers = PhoneNumbers;
            item.ResponsibleEmployees = SelectedResponsiblePersons?.Cast<ComboBoxItem>().Select(x => x.Id).ToList();
            item.ContractorManagers = SelectedContractorManagers?.Cast<ComboBoxItem>().Select(x => x.Id).ToList();
            item.Subdivisions = SelectedSubdivisions.Select(x => x.Id).ToList();
            item.ContractorId = SelectedContractor?.Id;
            item.Available = Available;
            item.Completed = Completed;

            return item;
        }

        private static IEnumerable<CallState> GetDefaultStates()
        {
            yield return CallState.New;
        }

        private void InitializeObservableCollections()
        {
            SelectedCompletedByEmployees = new List<object>();
            SelectedResponsiblePersons = new List<object>();
            SelectedSubdivisions = new ObservableCollection<Subdivision>();
            SelectedContractorManagers = new List<object>();
            SelectedCallStates = new ObservableCollection<CallState>(GetDefaultStates());
        }

        private async Task RefreshCallTypesAsync()
        {
            List<CallTypeDto> callTypes = await WebClient.ExecuteApiRequestAsync(new QueryCallTypes(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(callTypes, callTypesList))
            {
                return;
            }

            CallTypeDto selected = SelectedCallType;

            CallTypes.Clear();
            callTypesList = callTypes.OrderBy(x => x.Name).ToList();

            if (selected != null)
            {
                selected = callTypesList.FirstOrDefault(x => x.Id == selected.Id);
            }

            CallTypes.AddRange(callTypesList);
            SelectedCallType = selected;
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();

            employeesList = employees;

            IEnumerable<ComboBoxItem> employeeItems = employeesList
                .Where(x => x.Active && x.HasAnyRole(Role.Operator, Role.Manager))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue);

            Employees.AddRange(employeeItems);
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractorDtos = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractorDtos, contractorsList))
            {
                return;
            }

            Contractors.Clear();

            contractorsList = contractorDtos;

            IOrderedEnumerable<ContractorDto> contractorItems = contractorsList
                .Where(x => x.Active && !x.IsFolder && x.IsClient)
                .OrderBy(x => x.Name);

            Contractors.AddRange(contractorItems);
        }

        public CallFilteringItem GetFilteringItem()
        {
            return GetCallFilteringItem();
        }

        public void SetFilteringItem(CallFilteringItem filteringItem)
        {
            CallIds = filteringItem.CallIds;
            OrderIds = filteringItem.OrderIds;
            ServiceRequestIds = filteringItem.ServiceRequestIds;
            CallCreatedFrom = filteringItem.CallCreatedFrom;
            CallCreatedTo = filteringItem.CallCreatedTo;
            CallCompletedFrom = filteringItem.CallCompletedFrom;
            CallCompletedTo = filteringItem.CallCompletedTo;
            PhoneNumbers = filteringItem.PhoneNumbers;

            if (filteringItem.Subdivisions?.Count > 0)
            {
                SelectedSubdivisions = Subdivisions.Where(x => filteringItem.Subdivisions.Contains(x.Id)).ToObservableCollection();
            }

            SelectedContractor = Contractors.FirstOrDefault(x => x.Id == filteringItem.ContractorId);
            Fio = filteringItem.Fio;
            Available = filteringItem.Available ?? false;
            Completed = filteringItem.Completed ?? false;

            if (filteringItem.ContractorManagers?.Count > 0)
            {
                SelectedContractorManagers = Employees.Where(x => filteringItem.ContractorManagers.Contains(x.Id)).Select(x => (object)x).ToList();
            }

            if (filteringItem.ResponsibleEmployees?.Count > 0)
            {
                SelectedResponsiblePersons = Employees.Where(x => filteringItem.ResponsibleEmployees.Contains(x.Id)).Select(x => (object)x).ToList();
            }

            if (filteringItem.CompletedByEmployees?.Count > 0)
            {
                SelectedCompletedByEmployees = Employees.Where(x => filteringItem.CompletedByEmployees.Contains(x.Id)).Select(x => (object)x).ToList();
            }

            if (filteringItem.CallStates?.Count > 0)
            {
                SelectedCallStates = CallStates.Where(x => filteringItem.CallStates.Contains(x.Id)).ToObservableCollection();
            }

            if (filteringItem.CallTypes?.Count > 0)
            {
                SelectedCallType = callTypesList.FirstOrDefault(x => filteringItem.CallTypes.Contains(x.Id));
            }
        }
    }
}