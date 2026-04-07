using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.PromoCode
{
    public class PromoCodeFilterViewModel : BindableBase, IDataErrorInfo, IFilteringViewModel<PromoCodeFilteringItem>
    {
        private readonly IWebClient _webClient;
        private readonly IDictionaries _dictionaries;

        private List<EmployeeDto> employeesList;

        public PromoCodeFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            _webClient = webClient;
            _dictionaries = dictionaries;
        }

        public List<object> SelectedCreatedByEmployees
        {
            get { return GetProperty(() => SelectedCreatedByEmployees); }
            set { SetProperty(() => SelectedCreatedByEmployees, value); }
        }

        public bool? Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public List<object> SelectedTypes
        {
            get { return GetProperty(() => SelectedTypes); }
            set { SetProperty(() => SelectedTypes, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<PromoCodeType> Types { get; } = new ObservableRangeCollection<PromoCodeType>();

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public void ResetFilterValues()
        {
            Active = null;
            SelectedCreatedByEmployees = null;
            SelectedTypes = new[] { PromoCodeType.Bundle, PromoCodeType.ProductDiscount }.Cast<object>().ToList();
        }

        public PromoCodeFilteringItem GetFilteringItem()
        {
            PromoCodeFilteringItem item = new PromoCodeFilteringItem
            {
                Active = Active,
                CreatedByEmployeeIds = SelectedCreatedByEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                TypeIds = SelectedTypes?.Cast<PromoCodeType>().Select(x => x.Id).ToList()
            };

            return item;
        }

        public void SetFilteringItem(PromoCodeFilteringItem filteringItem)
        {
            throw new NotSupportedException();
        }

        public async Task RefreshAsync()
        {
            ResetFilterValues();

            Types.AddRange(_dictionaries.GetItems<PromoCodeType>());

            await RefreshEmployeesAsync();
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await _webClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(employees, employeesList))
            {
                return;
            }

            Employees.Clear();

            employeesList = employees;

            List<ComboBoxItem> employeeItems = employeesList
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Employees.AddRange(employeeItems);
        }
    }
}