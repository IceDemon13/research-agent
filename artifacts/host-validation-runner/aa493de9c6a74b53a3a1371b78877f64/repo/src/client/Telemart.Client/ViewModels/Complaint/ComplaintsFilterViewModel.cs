using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintsFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<EmployeeDto> employeesList;

        private List<ComplaintTypeDto> typesList;

        public ComplaintsFilterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries)
        {
            WebClient = webClient;

            States.AddRange(dictionaries.GetItems<ComplaintState>());
        }

        public ComplaintsFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComplaintTypeDto> Types { get; } = new ObservableRangeCollection<ComplaintTypeDto>();

        public ObservableRangeCollection<ComplaintState> States { get; } = new ObservableRangeCollection<ComplaintState>();

        #endregion

        public string ComplaintIds
        {
            get { return GetProperty(() => ComplaintIds); }
            set { SetProperty(() => ComplaintIds, value); }
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

        public string TradeInIds
        {
            get { return GetProperty(() => TradeInIds); }
            set { SetProperty(() => TradeInIds, value); }
        }

        public DateTime? CreatedFrom
        {
            get { return GetProperty(() => CreatedFrom); }
            set { SetProperty(() => CreatedFrom, value); }
        }

        public DateTime? CreatedTo
        {
            get { return GetProperty(() => CreatedTo); }
            set { SetProperty(() => CreatedTo, value); }
        }

        public ComplaintTypeDto SelectedType
        {
            get { return GetProperty(() => SelectedType); }
            set { SetProperty(() => SelectedType, value); }
        }

        public ObservableCollection<int> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public ObservableCollection<int> SelectedEmployeeIds
        {
            get { return GetProperty(() => SelectedEmployeeIds); }
            set { SetProperty(() => SelectedEmployeeIds, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ComplaintsFilterViewModel> builder)
        {
            builder.Property(x => x.ComplaintIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");

            builder.Property(x => x.OrderIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");

            builder.Property(x => x.ServiceRequestIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public ComplaintsFilteringItem GetFilteringItem()
        {
            ComplaintsFilteringItem item = new ComplaintsFilteringItem
            {
                ServiceRequestIds = ServiceRequestIds,
                ComplaintIds = ComplaintIds,
                OrderIds = OrderIds,
                Types = SelectedType == null
                    ? new List<int>()
                    : new List<int> { SelectedType.Id },
                States = SelectedStates?.ToList(),
                CreatedFrom = CreatedFrom,
                CreatedTo = CreatedTo,
                EmployeeIds = SelectedEmployeeIds?.ToList(),
                Fio = Fio,
                Phone = Phone
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(RefreshEmployeesAsync(), RefreshTypesAsync());
        }

        public void ResetFilterValues()
        {
            ServiceRequestIds = null;
            ComplaintIds = null;
            OrderIds = null;
            SelectedType = null;
            SelectedStates = new[] { ComplaintState.New.Id }.ToObservableCollection();
            CreatedFrom = null;
            CreatedTo = null;
            SelectedEmployeeIds = null;
            Fio = null;
            Phone = null;
        }

        public async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();

            employeesList = employees;

            List<ComboBoxItem> employeeItems = employeesList.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Employees.AddRange(employeeItems);
        }

        public async Task RefreshTypesAsync()
        {
            List<ComplaintTypeDto> types = await WebClient.ExecuteApiRequestAsync(new QueryComplaintTypes(), true);

            if (ReferenceEquals(types, typesList))
            {
                return;
            }

            Types.Clear();

            typesList = types;

            Types.AddRange(types
                .Where(x => x.Active)
                .OrderBy(x => x.Position)
                .ThenBy(x => x.Name));
        }
    }
}
