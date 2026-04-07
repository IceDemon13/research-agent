using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Tasks
{
    public sealed class TasksViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public TasksViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedTask != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Filter = new TasksFilterViewModel(webClient, dictionaries);

            Tasks = new ObservableRangeCollection<TaskViewItem>();

            Messenger.Register<TaskMessage>(this, OnBacklogTaskMessage);
        }

        public TasksViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public TasksFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<TaskState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<TaskViewItem> Tasks
        {
            get { return GetProperty(() => Tasks); }
            set { SetProperty(() => Tasks, value); }
        }

        public TaskViewItem SelectedTask
        {
            get { return GetProperty(() => SelectedTask); }
            set { SetProperty(() => SelectedTask, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService", ServiceSearchMode.PreferParents);

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
            IsSearchPanelClosed = false;

            States = Dictionaries.GetItems<TaskState>().ToReadOnlyObservableCollection();

            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            NonModalDialogDocumentManagerService.ShowEditorView<TaskViewModel>(SelectedTask.Id, new TaskParameter(SelectedTask.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await Task.WhenAll(RefreshEmployees(), RefreshTypes());

                await Filter.RefreshAsync();

                PagedResult<TaskDto> tasks = await WebClient.ExecuteApiRequestAsync(new QueryTasks(Filter.GetFilteringItem()));

                Tasks.Clear();
                Tasks.AddRange(tasks.Data.Select(x => Mapper.Map<TaskViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                Employees = employees
                    .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshTypes()
            {
                List<TaskTypeDto> taskTypes = await WebClient.ExecuteApiRequestAsync(new QueryTaskTypes(), true);

                Types = taskTypes
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        private void OnBacklogTaskMessage(TaskMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Tasks.Insert(0, Mapper.Map<TaskViewItem>(message.Entity));

                    break;
                case MessageType.Changed:
                    Tasks.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));

                    break;
            }
        }
    }
}
