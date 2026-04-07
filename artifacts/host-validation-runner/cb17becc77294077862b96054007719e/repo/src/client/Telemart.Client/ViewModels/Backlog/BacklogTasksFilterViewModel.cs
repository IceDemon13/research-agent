using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTasksFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<EmployeeDto> employeesList;
        private List<BacklogCategoryDto> categoriesList;

        public BacklogTasksFilterViewModel(IWebClient webClient, IDictionaries dictionaries, IMapper mapper)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            Statuses.AddRange(Dictionaries.GetItems<BacklogTaskState>());
        }

        public BacklogTasksFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Employees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<ComboBoxItem> ResponsibleEmployees { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<BacklogTaskState> Statuses { get; } = new ObservableRangeCollection<BacklogTaskState>();

        #endregion

        public string TaskNumbers
        {
            get { return GetProperty(() => TaskNumbers); }
            set { SetProperty(() => TaskNumbers, value); }
        }

        public string BitrixIds
        {
            get { return GetProperty(() => BitrixIds); }
            set { SetProperty(() => BitrixIds, value); }
        }

        public List<object> SelectedAuthor
        {
            get { return GetProperty(() => SelectedAuthor); }
            set { SetProperty(() => SelectedAuthor, value); }
        }

        public List<object> SelectedEmployees
        {
            get { return GetProperty(() => SelectedEmployees); }
            set { SetProperty(() => SelectedEmployees, value); }
        }

        public ObservableCollection<BacklogTaskState> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        public ObservableCollection<BacklogCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private IMapper Mapper { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<BacklogTasksFilterViewModel> builder)
        {
            builder.Property(x => x.TaskNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");

            builder.Property(x => x.BitrixIds)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public BacklogFilteringItem GetBacklogFilteringItem()
        {
            BacklogFilteringItem item = new BacklogFilteringItem
            {
                TaskNumbers = TaskNumbers,
                BitrixIds = BitrixIds,
                Statuses = SelectedStatuses?.Select(x => x.Id).ToList(),
                AuthorIds = SelectedAuthor?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                EmployeeIds = SelectedEmployees?.Cast<ComboBoxItem>().Select(x => x.Id).ToList(),
                CategoryIds = Categories.Where(x => x.Checked == true).Select(x => x.Id).ToList()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return Task.WhenAll(RefreshEmployeesAsync(), RefreshCategoriesAsync());
        }

        public void ResetFilterValues()
        {
            TaskNumbers = null;
            BitrixIds = null;
            SelectedStatuses = new ObservableCollection<BacklogTaskState> { BacklogTaskState.Idea, BacklogTaskState.Specify, BacklogTaskState.Formulated, BacklogTaskState.Documented, BacklogTaskState.InProgress };
            SelectedAuthor = new List<object>();
            SelectedEmployees = new List<object>();
            Categories.ForEach(x => x.Checked = false);
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

            List<ComboBoxItem> employeeItems = employeesList.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Employees.AddRange(employeeItems);

            await RefreshResponsibleEmployeesAsync(employeeItems);
        }

        private async Task RefreshCategoriesAsync()
        {
            List<BacklogCategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryBacklogCategories(), true);

            if (ReferenceEquals(categories, categoriesList))
            {
                return;
            }

            IReadOnlyDictionary<int, bool?> selectedCategories = Categories?
                .ToDictionary(x => x.Id, x => x.Checked);

            categoriesList = categories;

            Categories = categoriesList
                .Where(x => x.Active)
                .OrderBy(x => x.Name)
                .Select(x => Mapper.Map<BacklogCategoryViewItem>(x))
                .ToObservableCollection();

            if (selectedCategories is null)
            {
                Categories.ForEach(x => x.Checked = true);
            }
            else
            {
                Categories.ForEach(x => x.Checked = selectedCategories.GetValueOrDefault(x.Id, true));
            }
        }

        private async Task RefreshResponsibleEmployeesAsync(List<ComboBoxItem> employeeItems)
        {
            int userId = WebClient.AuthenticatedEmployee.Id;

            ResponsibleEmployees.Clear();

            if (WebClient.IsOperationAllowed(BusinessOperation.BacklogTaskGetAllTasks))
            {
                ResponsibleEmployees.AddRange(employeeItems);
                return;
            }

            ResponsibleEmployees.Add(employeeItems.FirstOrDefault(x => x.Id == userId));

            List<DepartmentDto> departments = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            DepartmentDto department = departments?.FirstOrDefault(x => x.EmployeeId == WebClient.AuthenticatedEmployee.Id);

            ResponsibleEmployees.AddRange(employeeItems.Where(x => department?.Employees?.Contains(x.Id) == true && x.Id != userId));
        }
    }
}