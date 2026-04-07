using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Notifications;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Notification;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Notification
{
    public sealed class NotificationsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IViewModelResolver _viewModelResolver;
        private readonly IMessenger _messenger;
        private readonly IMapper _mapper;

        public NotificationsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IViewModelResolver viewModelResolver,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _viewModelResolver = viewModelResolver;
            _messenger = messenger;
            _mapper = mapper;

            Filter = new NotificationsFilterViewModel(webClient);

            RefreshCommand = new AsyncCommand(RefreshAsync);
            OpenSubscribesEditorCommand = new DelegateCommand(OpenSubscribesEditor);
            OpenDocumentCommand = new DelegateCommand(OpenDocument, () => SelectedNotification is not null);
            CancelFilteringCommand = new DelegateCommand(Filter.Reset);
        }

        public NotificationsFilterViewModel Filter { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand OpenSubscribesEditorCommand { get; }

        public IDelegateCommand OpenDocumentCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public NotificationViewItem SelectedNotification
        {
            get { return GetProperty(() => SelectedNotification); }
            set { SetProperty(() => SelectedNotification, value); }
        }

        public ObservableCollection<NotificationViewItem> Notifications
        {
            get { return GetProperty(() => Notifications); }
            set { SetProperty(() => Notifications, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> NotificationTypes
        {
            get { return GetProperty(() => NotificationTypes); }
            set { SetProperty(() => NotificationTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> NotificationEntities
        {
            get { return GetProperty(() => NotificationEntities); }
            set { SetProperty(() => NotificationEntities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Targets
        {
            get { return GetProperty(() => Targets); }
            set { SetProperty(() => Targets, value); }
        }

        public ReadOnlyObservableCollection<DepartmentDto> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(FetchEmployeesAsync(), FetchNotificationTypesAsync(), FetchDepartmentAsync());

            NotificationEntities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .ToReadOnlyObservableCollection();

            Targets = Dictionaries
                .GetItems<NotificationTarget>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Filter.Init(Targets, GetFilteredEmployees(), NotificationTypes, NotificationEntities);

            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            PagedResult<NotificationDto> notifications =
                await WebClient.ExecuteApiRequestAsync(new QueryNotifications(Filter.GetFilteringItem()));

            Notifications = notifications.Data
                .Select(x => _mapper.Map<NotificationViewItem>(x))
                .ToObservableCollection();
        }

        private async Task FetchEmployeesAsync()
        {
            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Employees = employees.Data.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task FetchNotificationTypesAsync()
        {
            List<NotificationTypeDto> notificationTypes = await WebClient.ExecuteApiRequestAsync(new QueryNotificationTypes());

            NotificationTypes = notificationTypes
                .Select(x => new ComboBoxItem(x.Id, x.Text))
                .ToReadOnlyObservableCollection();
        }

        private async Task FetchDepartmentAsync()
        {
            List<DepartmentDto> departments = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            Departments = departments.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private void OpenSubscribesEditor()
        {
            DialogDocumentManagerService.ShowView<NotificationsSubscribesViewModel>(null, this);
        }

        private void OpenDocument()
        {
            if (SelectedNotification.DocumentId is null or <= 0)
            {
                return;
            }

            (bool ViewModelSupport, object Message) response = _viewModelResolver.Resolve(SelectedNotification.EntityId, SelectedNotification.DocumentId.Value);

            if (!response.ViewModelSupport)
            {
                MessageFacadeService.ShowNotificationWarning("Документ не поддерживает открытия");
                return;
            }

            ViewModelOpenHelper.OpenByMessage(response.Message, _messenger, this);
        }

        private ReadOnlyObservableCollection<ComboBoxItem> GetFilteredEmployees()
        {
            if (WebClient.IsOperationAllowed(BusinessOperation.NotificationsGetAll))
            {
                return Employees;
            }

            int userId = WebClient.AuthenticatedEmployee.Id;

            DepartmentDto department = Departments?.FirstOrDefault(x => x.EmployeeId == userId);

            int[] employeeIds = department?.Employees;

            return Employees.Where(x => x.Id == userId || employeeIds?.Contains(x.Id) == true)
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }
    }
}