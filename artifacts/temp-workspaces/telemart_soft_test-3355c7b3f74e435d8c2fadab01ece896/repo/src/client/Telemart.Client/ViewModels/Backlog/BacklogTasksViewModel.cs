using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Quotas;

namespace Telemart.Client.ViewModels.Backlog
{
    internal sealed class BacklogTasksViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<BacklogTaskViewItem> _taskList;
        private IReadOnlyDictionary<int, EmployeeDto> _employees;

        public BacklogTasksViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            DocumentCommands documentCommands,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;
            DocumentCommands = documentCommands;

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.BacklogTaskCreate);

            AddCommand = new DelegateCommand(Add);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedTask != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            OpenQuotasCommand = new DelegateCommand(OpenQuotas, () => WebClient.IsOperationAllowed(BusinessOperation.QuotasTool));
            ShowFilterPopupHandlerCommand = new DelegateCommand<FilterPopupEventArgs>(ShowFilterPopupHandler);


            Filter = new BacklogTasksFilterViewModel(webClient, dictionaries, mapper);

            Tasks = new ObservableRangeCollection<BacklogTaskViewItem>();

            Messenger.Register<BacklogTaskMessage>(this, OnBacklogTaskMessage);
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand OpenQuotasCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand ShowFilterPopupHandlerCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion

        #region INPC

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public BacklogTasksFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ActiveEmployees
        {
            get { return GetProperty(() => ActiveEmployees); }
            private set { SetProperty(() => ActiveEmployees, value); }
        }

        public ObservableRangeCollection<BacklogTaskViewItem> Tasks
        {
            get { return GetProperty(() => Tasks); }
            set { SetProperty(() => Tasks, value); }
        }

        public BacklogTaskViewItem SelectedTask
        {
            get { return GetProperty(() => SelectedTask); }
            set { SetProperty(() => SelectedTask, value, MarkCategories); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool ShowOnlyFavorite
        {
            get { return GetProperty(() => ShowOnlyFavorite); }
            set { SetProperty(() => ShowOnlyFavorite, value, FilterTasks); }
        }

        public ReadOnlyObservableCollection<BacklogCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public BacklogCategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, FilterTasks); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NotModalSizeableDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.Add:

                        if (CanCreate)
                        {
                            AddCommand.Execute(null);
                            handled = true;
                        }

                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            _taskList = new List<BacklogTaskViewItem>();

            IsSearchPanelClosed = false;

            await Filter.RefreshAsync();
            Filter.ResetFilterValues();

            await RefreshAsync();
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            CanCreate = true;
        }

        private void Add()
        {
            NotModalSizeableDocumentManagerService.ShowView<CreateBacklogTaskViewModel>(null, this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            Messenger.Send(new BacklogTaskViewMessage(SelectedTask.Id));
        }

        private async Task RefreshAsync()
        {
            try
            {
                await Task.WhenAll(RefreshEmployeesAsync(), DocumentCommands.InitAsync(this, DialogDocumentManagerService), Filter.RefreshAsync());

                PagedResult<BacklogTaskDto> tasks = await WebClient.ExecuteApiRequestAsync(new QueryBacklogTasks(Filter.GetBacklogFilteringItem()));

                _taskList = tasks.Data.Select(x => MapTask(x)).ToList();

                int[] categoryIds = _taskList.SelectMany(x => x.CategoryIds).Distinct().ToArray();

                Categories = Filter.Categories.Where(x => categoryIds.Contains(x.Id)).ToReadOnlyObservableCollection();

                FilterTasks();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to refresh backlog tasks");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                _employees = employees.ToDictionary(x => x.Id);

                AllEmployees = employees
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();

                ActiveEmployees = employees
                    .Where(x => x.Active)
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();
            }
        }

        private void FilterTasks()
        {
            Tasks.Clear();
            Tasks.AddRange(_taskList.Where(TaskIsShow));
        }

        private bool TaskIsShow(BacklogTaskViewItem task)
        {
            return (!ShowOnlyFavorite || task.IsFavorite)
                && (SelectedCategory == null || task.CategoryIds.Contains(SelectedCategory.Id));
        }

        private void MarkCategories()
        {
            if (SelectedTask == null)
            {
                Categories.ForEach(x => x.Marked = false);
            }
            else
            {
                Categories.ForEach(x => x.Marked = SelectedTask.CategoryIds.Contains(x.Id));
            }
        }

        private BacklogTaskViewItem MapTask(BacklogTaskDto dto, BacklogTaskViewItem viewItem = null)
        {
            viewItem ??= new BacklogTaskViewItem();

            Mapper.Map(dto, viewItem);

            viewItem.IsFavorite = viewItem.FavoriteEmployeeIds.Contains(WebClient.AuthenticatedEmployee.Id);

            if (dto.EmployeeId.HasValue)
            {
                var employee = _employees[dto.EmployeeId.Value];

                viewItem.Employee = new ComboBoxItem(employee.Id, employee.Name, employee.Active);
            }
            else
            {
                viewItem.Employee = null;
            }

            return viewItem;
        }

        private void OnBacklogTaskMessage(BacklogTaskMessage message)
        {
            BacklogTaskViewItem newViewItem = MapTask(message.Entity);

            switch (message.MessageType)
            {
                case MessageType.Added:
                    _taskList.Insert(0, newViewItem);

                    if (TaskIsShow(newViewItem))
                    {
                        Tasks.Insert(0, newViewItem);
                    }

                    break;
                case MessageType.Changed:
                    _taskList.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => MapTask(message.Entity, viewItem));

                    if (TaskIsShow(newViewItem))
                    {
                        Tasks.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => MapTask(message.Entity, viewItem));
                    }
                    else
                    {
                        Tasks.RemoveAll(x => x.Id == newViewItem.Id);
                    }

                    break;
            }
        }

        private void ShowFilterPopupHandler(FilterPopupEventArgs e)
        {
            switch (e.Column.FieldName)
            {
                case nameof(BacklogTaskViewItem.EmployeeId):

                    if (e.ComboBoxEdit.ItemsSource is List<object> items)
                    {
                        List<object> newItems = new List<object>(items);

                        newItems.Insert(0, new CustomComboBoxItem { DisplayValue = "(Не задано)", EditValue = string.Empty });

                        e.ComboBoxEdit.ItemsSource = newItems;
                    }

                    break;
            }
        }

        private void OpenQuotas()
        {
            SizeableDialogDocumentManagerService.ShowView<QuotasViewModel>(null, this);
        }
    }
}