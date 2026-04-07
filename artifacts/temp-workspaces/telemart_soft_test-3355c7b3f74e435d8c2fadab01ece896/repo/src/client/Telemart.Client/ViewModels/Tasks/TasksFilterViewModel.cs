using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.ViewModels.Tasks
{
    public sealed class TasksFilterViewModel : BindableBase, IDataErrorInfo
    {
        private List<TaskTypeDto> taskTypesList;

        public TasksFilterViewModel(IWebClient webClient, IDictionaries dictionaries)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));

            States.AddRange(Dictionaries.GetItems<TaskState>());
        }

        public TasksFilterViewModel()
        {
        }

        #region Collections

        public ObservableRangeCollection<ComboBoxItem> Types { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<TaskState> States { get; } = new ObservableRangeCollection<TaskState>();

        #endregion

        public string Numbers
        {
            get { return GetProperty(() => Numbers); }
            set { SetProperty(() => Numbers, value); }
        }

        public List<object> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public List<object> SelectedTypes
        {
            get { return GetProperty(() => SelectedTypes); }
            set { SetProperty(() => SelectedTypes, value); }
        }

        string IDataErrorInfo.Error => string.Empty;

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<TasksFilterViewModel> builder)
        {
            builder.Property(x => x.Numbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public TaskFilteringItem GetFilteringItem()
        {
            TaskFilteringItem item = new TaskFilteringItem
            {
                Numbers = Numbers,
                States = SelectedStates?.Cast<int>().ToList(),
                Types = SelectedTypes?.Cast<int>().ToList()
            };

            return item;
        }

        public Task RefreshAsync()
        {
            return RefreshTypesAsync();
        }

        public void ResetFilterValues()
        {
            Numbers = null;
            SelectedStates = new List<object> { TaskState.New.Id, TaskState.InProgress.Id };
        }

        private async Task RefreshTypesAsync()
        {
            List<TaskTypeDto> taskTypes = await WebClient.ExecuteApiRequestAsync(new QueryTaskTypes(), true);

            if (ReferenceEquals(taskTypes, taskTypesList))
            {
                return;
            }

            Types.Clear();

            taskTypesList = taskTypes;

            List<ComboBoxItem> employeeItems = taskTypes
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToList();

            Types.AddRange(employeeItems);
        }
    }
}
