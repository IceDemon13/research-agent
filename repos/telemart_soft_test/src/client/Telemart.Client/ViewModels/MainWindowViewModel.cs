using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Cache;
using Telemart.Client.Common;
using Telemart.Client.Common.HubFactory;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Navigation;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Core;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Core.Update;
using Telemart.Client.Data;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Diagnostics;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.HubClient.Hubs;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Monitoring;
using Telemart.Client.Data.Requests.Features.Notifications;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Jobs;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.PosTerminal.PrivatBank;
using Telemart.Client.SingleInstance;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs.AddDocuments;
using Telemart.Client.ViewModels.Notification;
using Telemart.Client.ViewModels.Parser.Monitoring;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.Views.Settings.Printing;
using Telemart.Client.Views.Tasks;

namespace Telemart.Client.ViewModels
{
    internal sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly SingleInstanceAppProcessor _singleInstanceAppProcessor;
        private readonly UpdateManagerOptions _updateManagerOptions;
        private readonly CallTrackOptions _callTrackOptions;
        private readonly ILiteDbConnectionFactory _liteDbConnectionFactory;
        private readonly INetworkDiagnoser _networkDiagnoser;
        private readonly SyncCacheJob _syncCacheJob;
        private IJobDetail _checkUpdateJobDetail;
        private IJobDetail _checkNewCommentsJobDetail;
        private IJobDetail _getCurrencyRatesJobDetail;
        private IJobDetail _updateAuthenticatedEmployeeJobDetail;
        private IJobDetail _getUserNotificationsJobDetail;
        private IJobDetail _showPhoneHistoryJobDetail;
        private bool _needsConfirmationOnClose = true;
        private DispatcherTimer _notificationBlinkTimer;
        private DispatcherTimer _forceUpdateTimer;
        private DispatcherTimer _lockClientTimer;
        private TimeSpan _forceUpdateTime;
        private DateTime? _yellowConnectionSetTime;
        private bool _startInitingWithRefreshToken;
        private bool _showsFirstTime = true;
        private bool _equipmentSettingsCheck;

        public MainWindowViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IUpdateManager updateManager,
            ISchedulerFactory schedulerFactory,
            INetworkDiagnoser networkDiagnoser,
            IViewModelResolver viewModelResolver,
            IAuthenticationManager authenticationManager,
            SingleInstanceAppProcessor singleInstanceAppProcessor,
            IEquipmentSettingsStore equipmentSettingsStore,
            IHubClientFactory hubClientFactory,
            IEquipmentSettingsWorker equipmentSettingsWorker,
            INavigationMenuBuilder navigationMenuBuilder,
            UpdateManagerOptions updateManagerOptions,
            CallTrackOptions callTrackOptions,
            WorkspaceViewModel workspaceViewModel,
            LoginViewModel loginViewModel,
            ILogger<MainWindowViewModel> logger,
            IMediator mediator,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILiteDbConnectionFactory liteDbConnectionFactory,
            SyncCacheJob syncCacheJob)
            : this()
        {
            _singleInstanceAppProcessor = singleInstanceAppProcessor;
            EquipmentSettingsStore = equipmentSettingsStore;
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            UpdateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));
            SchedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            AuthenticationManager = authenticationManager;
            NotificationHubClient = hubClientFactory.Create<NotificationHub>(messageFacadeService, stateHandling: true);
            NavigationMenuBuilder = navigationMenuBuilder;
            EquipmentSettingsWorker = equipmentSettingsWorker;
            WorkspaceViewModel = workspaceViewModel;
            LoginViewModel = loginViewModel;
            Logger = logger;
            ViewModelResolver = viewModelResolver;
            ServiceProvider = serviceProvider;
            _updateManagerOptions = updateManagerOptions;
            _callTrackOptions = callTrackOptions;
            Mediator = mediator;
            Mapper = mapper;
            _liteDbConnectionFactory = liteDbConnectionFactory;
            _syncCacheJob = syncCacheJob;

            Messenger.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
            Messenger.Register<UpdateAvailableMessage>(this, OnUpdateAvailable);
            Messenger.Register<UpdateCurrencyRatesMessage>(this, OnUpdateCurrencyRates);
            Messenger.Register<UpdateWorkPlaceMessage>(this, OnUpdateWorkPlace);
            Messenger.Register<UserCancelLoginMessage>(this, OnUserCancelLogin);
            Messenger.Register<UpdateUserNotificationsMessage>(this, OnUpdateUserNotifications);
            Messenger.Register<TaskMessage>(this, OnTaskMessage);
            Messenger.Register<ShowNewCommentNotificationMessage>(this, OnShowNewCommentMessage);
            Messenger.Register<LockTimeoutChangedMessage>(this, OnLockTimeoutChanged);

            NotificationHubClient.RegisterHandler<NotifyAllDto>(NotificationHub.GetNotificationMethod, ShowNotification);

            SubscribeOnHubReconnections();

            _networkDiagnoser = networkDiagnoser;
            _networkDiagnoser.StateChanged += OnNetworkStateChanged;

            Navigate(ModuleType.None);
        }

        public MainWindowViewModel()
        {
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            LogoutCommand = new AsyncCommand(() => LockAsync(true), IsUserAuthenticated);
            ShowUserInfoCommand = new DelegateCommand(ShowUserInfo, IsUserAuthenticated);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandlePreviewMouseDownCommand = new DelegateCommand<MouseButtonEventArgs>(HandlePreviewMouseDown);
            HandleClosingCommand = new AsyncCommand<CancelEventArgs>(HandleClosingAsync);
            UpdateCurrencyRatesCommand = new DelegateCommand(UpdateCurrencyRates);
            ShowWorkPlaceCommand = new DelegateCommand(ShowWorkPlace);
            ShowNotificationsCommand = new DelegateCommand(ShowNotifications);
            ForceUpdateCommand = new DelegateCommand(ForceUpdate);
            NotifyAllCommand = new DelegateCommand(NotifyAll);
            ActiveClientsCommand = new AsyncCommand(ActiveClientsAsync);
            SettingsCommand = new DelegateCommand(ShowSettings);

            Title = "Telemart.Client";
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandlePreviewMouseDownCommand { get; }

        public IDelegateCommand SettingsCommand { get; }

        public IAsyncCommand LogoutCommand { get; }

        public IDelegateCommand ShowUserInfoCommand { get; }

        public IDelegateCommand NotifyAllCommand { get; }

        public IAsyncCommand ActiveClientsCommand { get; }

        public IAsyncCommand HandleClosingCommand { get; }

        public IDelegateCommand UpdateCurrencyRatesCommand { get; }

        public IDelegateCommand ShowWorkPlaceCommand { get; }

        public IDelegateCommand ShowNotificationsCommand { get; }

        public IDelegateCommand ForceUpdateCommand { get; }

        public static bool ChangeUsersStatusAsterisk { get; set; }

        #endregion

        public ReadOnlyObservableCollection<string> AuthenticatedEmployeeRoles
        {
            get { return GetProperty(() => AuthenticatedEmployeeRoles); }
            private set { SetProperty(() => AuthenticatedEmployeeRoles, value, () => { RaisePropertyChanged(nameof(CanUpdateCurrencyRates)); }); }
        }

        public ReadOnlyObservableCollection<BusinessOperation> AuthenticatedEmployeeOperations
        {
            get { return GetProperty(() => AuthenticatedEmployeeOperations); }
            private set { SetProperty(() => AuthenticatedEmployeeOperations, value, () => { RaisePropertyChanged(nameof(CanUpdateCurrencyRates)); }); }
        }

        public string AuthenticatedUserName
        {
            get { return GetProperty(() => AuthenticatedUserName); }
            private set { SetProperty(() => AuthenticatedUserName, value); }
        }

        public string Title
        {
            get { return GetProperty(() => Title); }
            set { SetProperty(() => Title, value); }
        }

        public int UpdateProgress
        {
            get { return GetProperty(() => UpdateProgress); }
            set { SetProperty(() => UpdateProgress, value); }
        }

        public string UpdateProgressText
        {
            get { return GetProperty(() => UpdateProgressText); }
            set { SetProperty(() => UpdateProgressText, value); }
        }

        public bool UpdateReadyVisible
        {
            get { return GetProperty(() => UpdateReadyVisible); }
            set { SetProperty(() => UpdateReadyVisible, value); }
        }

        public bool UpdateStatusVisible
        {
            get { return GetProperty(() => UpdateStatusVisible); }
            set { SetProperty(() => UpdateStatusVisible, value); }
        }

        public bool ForceUpdateVisible
        {
            get { return GetProperty(() => ForceUpdateVisible); }
            set { SetProperty(() => ForceUpdateVisible, value); }
        }

        public bool ForceUpdateButtonVisible
        {
            get { return GetProperty(() => ForceUpdateButtonVisible); }
            set { SetProperty(() => ForceUpdateButtonVisible, value); }
        }

        public string ForceUpdateText
        {
            get { return GetProperty(() => ForceUpdateText); }
            set { SetProperty(() => ForceUpdateText, value); }
        }

        public Brush ForceUpdateForeground
        {
            get { return GetProperty(() => ForceUpdateForeground); }
            set { SetProperty(() => ForceUpdateForeground, value); }
        }

        public string CurrencyReatesText
        {
            get { return GetProperty(() => CurrencyReatesText); }
            set { SetProperty(() => CurrencyReatesText, value); }
        }

        public string WorkPlaceText
        {
            get { return GetProperty(() => WorkPlaceText); }
            set { SetProperty(() => WorkPlaceText, value); }
        }

        public string NotificationItemText
        {
            get { return GetProperty(() => NotificationItemText); }
            set { SetProperty(() => NotificationItemText, value); }
        }

        public bool CloseTrigger
        {
            get { return GetProperty(() => CloseTrigger); }
            private set { SetProperty(() => CloseTrigger, value); }
        }

        public bool LoginViewVisible
        {
            get { return GetProperty(() => LoginViewVisible); }
            private set { SetProperty(() => LoginViewVisible, value); }
        }

        public bool UpdateViewVisible
        {
            get { return GetProperty(() => UpdateViewVisible); }
            private set { SetProperty(() => UpdateViewVisible, value); }
        }

        public bool NotifyAllVisible
        {
            get { return GetProperty(() => NotifyAllVisible); }
            private set { SetProperty(() => NotifyAllVisible, value); }
        }

        public bool ActiveClientsVisible
        {
            get { return GetProperty(() => ActiveClientsVisible); }
            private set { SetProperty(() => ActiveClientsVisible, value); }
        }

        public bool SettingsVisible
        {
            get { return GetProperty(() => SettingsVisible); }
            private set { SetProperty(() => SettingsVisible, value); }
        }

        public bool WorkspaceViewVisible
        {
            get { return GetProperty(() => WorkspaceViewVisible); }
            private set { SetProperty(() => WorkspaceViewVisible, value); }
        }

        public bool UserNotificationBlink
        {
            get { return GetProperty(() => UserNotificationBlink); }
            private set { SetProperty(() => UserNotificationBlink, value); }
        }

        public bool UserNotificationsExists
        {
            get { return GetProperty(() => UserNotificationsExists); }
            private set { SetProperty(() => UserNotificationsExists, value); }
        }

        public HubConnectionState HubConnectionState
        {
            get { return GetProperty(() => HubConnectionState); }
            private set { SetProperty(() => HubConnectionState, value); }
        }

        public LoginViewModel LoginViewModel
        {
            get { return GetProperty(() => LoginViewModel); }
            private set { SetProperty(() => LoginViewModel, value); }
        }

        public WorkspaceViewModel WorkspaceViewModel
        {
            get { return GetProperty(() => WorkspaceViewModel); }
            private set { SetProperty(() => WorkspaceViewModel, value); }
        }

        public string DownloadSpeedNetworkStateString
        {
            get { return GetProperty(() => DownloadSpeedNetworkStateString); }
            private set { SetProperty(() => DownloadSpeedNetworkStateString, value); }
        }

        public string DownloadSpeedNetworkStateToolTip
        {
            get { return GetProperty(() => DownloadSpeedNetworkStateToolTip); }
            private set { SetProperty(() => DownloadSpeedNetworkStateToolTip, value); }
        }

        public bool CanUpdateCurrencyRates => AuthenticatedEmployeeOperations?.Any(x => x == BusinessOperation.ReceiveMoney) == true;

        private ILogger<MainWindowViewModel> Logger { get; }

        private IAuthenticationManager AuthenticationManager { get; }

        private IHubClientBase<NotificationHub> NotificationHubClient { get; }

        private IMessenger Messenger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IScheduler Scheduler { get; set; }

        private ISchedulerFactory SchedulerFactory { get; }

        private IUpdateManager UpdateManager { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private INavigationMenuBuilder NavigationMenuBuilder { get; }

        private IViewModelResolver ViewModelResolver { get; }

        private IEquipmentSettingsStore EquipmentSettingsStore { get; }

        private IEquipmentSettingsWorker EquipmentSettingsWorker { get; }

        private IMediator Mediator { get; }

        private IMapper Mapper { get; }

        private IServiceProvider ServiceProvider { get; }

        public async Task InitWithRefreshTokenAsync(string refreshToken, ReadOnlyCollection<Cookie> cookies)
        {
            _startInitingWithRefreshToken = true;
            LoginViewVisible = false;

            AuthenticationManager.RefreshToken = refreshToken;

            await AuthenticationManager.RefreshTokensAsync(true);

            await WebClient.QueryAndSetAuthenticatedEmployeeAsync();

            UserLoggedInMessage message = new UserLoggedInMessage(
                WebClient.AuthenticatedEmployee.Id,
                WebClient.AuthenticatedEmployee.Name,
                WebClient.AuthenticatedEmployee.Roles,
                WebClient.AuthenticatedEmployee.AllowedOperations.Cast<BusinessOperation>().ToArray(),
                true,
                cookies);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Messenger.Send(message);
            });
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Title = "Telemart.Client.x.y.z.w";

            UpdateStatusVisible = true;
            ForceUpdateVisible = true;
            UpdateProgress = 50;
            UpdateProgressText = "14567 КБ / 99999 КБ";
            ForceUpdateText = "Клиент перезапустится через 5 минут";

            AuthenticatedEmployeeRoles = new[] { Role.Admin.Name }.ToReadOnlyObservableCollection();
            AuthenticatedUserName = "John Doe";

            CurrencyReatesText = "24.9/25.1";
        }

        private string GetNotificationItemText(int messageCount)
        {
            if (!UserNotificationsExists)
            {
                return "Нет задач";
            }

            string newStr = WordEndingHelper.GetWordByNumber(messageCount, new[] { "новая", "новых", "новых" });
            string tasksStr = WordEndingHelper.GetWordByNumber(messageCount, new[] { "задача", "задачи", "задач" });

            return $"{messageCount} {newStr} {tasksStr}";
        }

        private async Task ShowNotification(NotifyAllDto dto)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        MessageBoxImage image = dto.NotificationImageId switch
                        {
                            NotificationImage.ErrorId => MessageBoxImage.Error,
                            NotificationImage.InformationId => MessageBoxImage.Information,
                            NotificationImage.WarningId => MessageBoxImage.Warning,
                            _ => default
                        };

                        switch (dto.NotificationFormatId)
                        {
                            case (int)NotificationFormat.MessageBox:
                                MessageFacadeService.ShowMessageBox(dto.Text, dto.Header, MessageBoxButton.OK, image);
                                break;

                            case (int)NotificationFormat.Notification:

                                if (dto.Document is not null)
                                {
                                    Entity entity = Dictionaries.GetItemById<Entity>(dto.EntityId!.Value);

                                    AddDocumentsParameter parameter = new AddDocumentsParameter(
                                        dto.DocumentId!.Value,
                                        null,
                                        $"Загрузка документов ({entity.DisplayName} №{dto.DocumentId!.Value})",
                                        dto.Document);

                                    Type documentViewModelType = typeof(OrderViewModel).Assembly
                                        .GetTypes()
                                        .FirstOrDefault(x => x.IsSubclassOf(typeof(AddDocumentsViewModelBase))
                                                             && x.Name.Split("AddDocument").FirstOrDefault() == entity.Name);

                                    if (documentViewModelType is null)
                                    {
                                        MessageFacadeService.ShowNotificationError("Не удалось принять документ");

                                        return;
                                    }

                                    object openedAddDocumentsViewModelObj = WorkspaceViewModel.NonModalSizeableDialogDocumentManagerService.Documents.FirstOrDefault(x => x.Content is AddDocumentsViewModelBase)?.Content;

                                    if (openedAddDocumentsViewModelObj is null)
                                    {
                                        object addDocumentsViewModel = ServiceProvider.GetRequiredService(documentViewModelType);

                                        Application.Current.Dispatcher.Invoke(() => WorkspaceViewModel.NonModalSizeableDialogDocumentManagerService.ShowView("AddDocumentsView", addDocumentsViewModel, parameter, WorkspaceViewModel));

                                        MessageFacadeService.ShowNotification(dto.Text, image);
                                    }
                                    else
                                    {
                                        AddDocumentsViewModelBase openAddDocumentsViewModel = (AddDocumentsViewModelBase)openedAddDocumentsViewModelObj;

                                        AddDocumentsParameter openedViewModelParameter = openAddDocumentsViewModel.ViewModelParameter;

                                        if (openedViewModelParameter.DocumentId != dto.DocumentId)
                                        {
                                            MessageFacadeService.ShowNotificationWarning("У вас открыта форма для другого документа");
                                            return;
                                        }

                                        openAddDocumentsViewModel.Files = openAddDocumentsViewModel.Files
                                            .Union(new[] { new AddDocumentViewItem(dto.Document) })
                                            .ToReadOnlyObservableCollection();

                                        MessageFacadeService.ShowNotification(dto.Text, image);
                                    }
                                }
                                else
                                {
                                    (bool ViewModelSupport, object Message) response = ViewModelResolver.Resolve(dto.EntityId, dto.DocumentId ?? 0);

                                    if (response.ViewModelSupport)
                                    {
                                        Type genericType = response.Message.GetType();

                                        MethodInfo method = typeof(IMessageFacadeService)
                                            .GetMethod(nameof(MessageFacadeService.ShowEntityNotification))!
                                            .MakeGenericMethod(genericType);

                                        method.Invoke(MessageFacadeService, new[]
                                        {
                                            dto.NotificationId!.Value,
                                            dto.Header,
                                            dto.Text,
                                            response.Message,
                                            image
                                        });
                                    }
                                    else
                                    {
                                        MessageFacadeService.ShowNotification(dto.Text, image);
                                    }
                                }

                                break;
                        }
                    });

                if (dto.NotificationId.HasValue)
                {
                    await WebClient.ExecuteApiRequestAsync(new ReceiveNotification(dto.NotificationId.Value));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to receive notification");
            }
        }

        private void NotifyAll()
        {
            WorkspaceViewModel.DialogDocumentManagerService.ShowView<NotifyAllViewModel>(null, this);
        }

        private void ShowSettings()
        {
            Messenger.Send(new ShowModuleMessage(typeof(PrintingSettingsView)));
        }

        private void ShowNotifications()
        {
            if (!UserNotificationsExists)
            {
                MessageFacadeService.ShowNotificationWarning("Нет новых задач");
                return;
            }

            int? employeeId = WebClient.AuthenticatedEmployee?.Id;

            if (employeeId.HasValue)
            {
                Messenger.Send(new ShowModuleMessage(typeof(TasksView)));
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs args)
        {
            if (_lockClientTimer?.IsEnabled == true)
            {
                _lockClientTimer.Stop();
                _lockClientTimer.Start();
            }

            if (WorkspaceViewVisible)
            {
                Messenger.Send(new KeyEventMessage(args));
            }
        }

        private void HandlePreviewMouseDown(MouseButtonEventArgs args)
        {
            if (_lockClientTimer?.IsEnabled == true)
            {
                _lockClientTimer.Stop();
                _lockClientTimer.Start();
            }
        }

        private async Task ActiveClientsAsync()
        {
            List<ActiveClientDto> activeClients = await WebClient.ExecuteApiRequestAsync(new QueryActiveClients());

            string reportText = string.Join(string.Empty, activeClients.OrderBy(x => x.LoggedIn).Select(x => $"{x.LoggedIn:dd.MM HH:mm}  {x.EmployeeName}  {x.UserAgentVersion}\n"));

            ShowTextParameter parameter = new ShowTextParameter(
                $"Активные клиенты ({DateTime.Now:dd.MM HH:mm})",
                reportText,
                true);

            WorkspaceViewModel.DefaultPositionDialogDocumentManagerService.ShowView<ShowTextViewModel>(parameter, WorkspaceViewModel);
        }

        private async Task HandleClosingAsync(CancelEventArgs e)
        {
            if (await LogoutAsync(_needsConfirmationOnClose) == false)
            {
                e.Cancel = true;
                CloseTrigger = false;

                return;
            }

            NotificationHubClient.Dispose();
        }

        private async Task HandleLoadedAsync()
        {
            Version currentVersion = UpdateManager.GetCurrentVersion();

            Title = $"Telemart.Client.{currentVersion}";

            Navigate(ModuleType.Update);

            WinCheckForUpdateResult? result;

            Scheduler ??= await SchedulerFactory.GetScheduler();

            try
            {
                foreach (Process process in Process.GetProcessesByName("Update"))
                {
                    process.Kill();
                }

                result = await UpdateManager.CheckIfUpdateAvailableAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to check if update available on application start");

                MessageFacadeService.ShowMessageBox(
                    "Ошибка получения информации о обновлениях. Приложение будет закрыто.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Application.Current.Shutdown();

                return;
            }

            if (result == null && !_startInitingWithRefreshToken)
            {
                Navigate(ModuleType.Login);
            }
            else if (result!.Value.ReleasesToApply?.Any() == true)
            {
                Messenger.Send(new UpdateAvailableMessage(result.Value));
            }
            else
            {
                await ScheduleCheckUpdateJobAsync();

                if (!_startInitingWithRefreshToken)
                {
                    Navigate(ModuleType.Login);
                }
            }
        }

        private void Navigate(ModuleType moduleType)
        {
            LoginViewVisible = moduleType == ModuleType.Login;
            UpdateViewVisible = moduleType == ModuleType.Update;
            WorkspaceViewVisible = moduleType == ModuleType.Workspace;
        }

        private bool IsUserAuthenticated()
        {
            return IsInDesignMode || WebClient.AuthenticatedEmployee != null;
        }

        private async Task<bool> LogoutAsync(bool needConfirmation = true)
        {
            if (needConfirmation && !MessageFacadeService.Confirm("Вы уверены, что хотите выйти?"))
            {
                return false;
            }

            Messenger.Send(new ParserCronicleMessage(true));

            await StopJobsAsync();

            if (WorkspaceViewModel.CloseDocuments())
            {
                AuthenticatedEmployeeRoles = null;
                AuthenticatedEmployeeOperations = null;
                AuthenticatedUserName = null;
                NotifyAllVisible = false;
                ActiveClientsVisible = false;
                SettingsVisible = false;
                WebClient.ClearAuthenticationInfo();
                Navigate(ModuleType.Login);

                await StopTrackCallsAsync();

                IPrivatBankPosTerminalClient posPbClient = ServiceProvider.GetRequiredService<IPrivatBankPosTerminalClient>();

                posPbClient?.Kill();
            }
            else
            {
                await Scheduler.ResumeJob(_getCurrencyRatesJobDetail.Key);
                await Scheduler.ResumeJob(_getUserNotificationsJobDetail.Key);
                await Scheduler.ResumeJob(_updateAuthenticatedEmployeeJobDetail.Key);

                await StartTrackCallsAsync();
            }

            return true;
        }

        private async Task StopJobsAsync()
        {
            if (_getCurrencyRatesJobDetail != null)
            {
                await Scheduler.PauseJob(_getCurrencyRatesJobDetail.Key);
            }

            if (_checkNewCommentsJobDetail != null)
            {
                await Scheduler.PauseJob(_checkNewCommentsJobDetail.Key);
            }

            if (_getUserNotificationsJobDetail != null)
            {
                await Scheduler.PauseJob(_getUserNotificationsJobDetail.Key);
            }

            if (_updateAuthenticatedEmployeeJobDetail != null)
            {
                await Scheduler.PauseJob(_updateAuthenticatedEmployeeJobDetail.Key);
            }

            await StopTrackCallsAsync();
            await _syncCacheJob.StopAsync(default);
        }

        private async Task LockAsync(bool showConfirm)
        {
            if (showConfirm && !MessageFacadeService.Confirm("Вы уверены, что хотите выйти?"))
            {
                return;
            }


            Messenger.Send(new SetUnavailableAsterisStatusMessage());

            WebClient.SetWorkPlaceId(null);
            WorkspaceViewModel.HideDocuments();

            _lockClientTimer.Stop();

            Navigate(ModuleType.Login);

            await StopJobsAsync();
        }

        private void ForceUpdate()
        {
            _singleInstanceAppProcessor.WaitForNewClientSendTokenAndShutdown(AuthenticationManager.RefreshToken, WorkspaceViewModel.Cookies);

            UpdateManager.RunLatestVersionAsync();
        }

        private async void OnUpdateAvailable(UpdateAvailableMessage obj)
        {
            try
            {
                Version versionBeforeUpdate = UpdateManager.GetCurrentVersion();

                if (_checkUpdateJobDetail != null)
                {
                    await Scheduler.PauseJob(_checkUpdateJobDetail.Key);
                }

                UpdateProgressText = string.Empty;
                UpdateProgress = 0;
                UpdateStatusVisible = true;

                Progress<int> progress = new Progress<int>(x => UpdateProgress = x);

                await UpdateManager.UpdateAsync(progress);

                UpdateProgressText = string.Empty;
                UpdateProgress = 100;
                UpdateStatusVisible = false;

                if (IsUserAuthenticated())
                {
                    MessageFacadeService.ShowUpdateNotificationInfo("Загрузка обновления завершена. Перезапустите программу.");

                    Version newVersion = new Version(obj.UpdateResult.FutureVersion);

                    ForceUpdateButtonVisible = true;

                    if (newVersion.Minor > versionBeforeUpdate.Minor || newVersion.Major > versionBeforeUpdate.Major)
                    {
                        if (WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin) && !MessageFacadeService.Confirm("Перезагрузить клиент через 5 минут для установки новой версии?"))
                        {
                            UpdateReadyVisible = true;
                            return;
                        }

                        ForceUpdateVisible = true;
                        _forceUpdateTime = TimeSpan.FromMinutes(5);

                        ForceUpdateForeground = Brushes.Black;

                        _forceUpdateTimer = new DispatcherTimer(
                            new TimeSpan(0, 0, 1),
                            DispatcherPriority.Normal,
                            (_, _) =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ForceUpdateText = $"До перезапуска: {_forceUpdateTime:c}";

                                    if (_forceUpdateTime < TimeSpan.FromMinutes(3))
                                    {
                                        if (_forceUpdateTime < TimeSpan.FromMinutes(1))
                                        {
                                            ForceUpdateForeground = _forceUpdateTime.Seconds % 2 == 0 ? Brushes.Black : Brushes.OrangeRed;
                                        }
                                        else
                                        {
                                            ForceUpdateForeground = _forceUpdateTime.Seconds % 2 == 0 ? Brushes.Black : Brushes.DarkRed;
                                        }
                                    }

                                    if (_forceUpdateTime == TimeSpan.FromMinutes(1))
                                    {
                                        MessageFacadeService.ShowMessageBoxWarning("Обновление загружено. Перезапуск клиента и установка обновления через 1 минуту. Сохраните данные!");
                                    }

                                    if (_forceUpdateTime == TimeSpan.Zero)
                                    {
                                        _forceUpdateTimer.Stop();
                                        ForceUpdateVisible = false;

                                        _singleInstanceAppProcessor.WaitForNewClientSendTokenAndShutdown(AuthenticationManager.RefreshToken, WorkspaceViewModel.Cookies);

                                        UpdateManager.RunLatestVersionAsync();
                                    }

                                    _forceUpdateTime = _forceUpdateTime.Add(TimeSpan.FromSeconds(-1));
                                });
                            },
                            Application.Current.Dispatcher);

                        _forceUpdateTimer.Start();
                    }
                    else
                    {
                        UpdateReadyVisible = true;
                    }
                }
                else
                {
                    _needsConfirmationOnClose = false;

                    Application.Current.Shutdown();

                    await UpdateManager.RunLatestVersionAsync();
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to download update");

                MessageFacadeService.ShowNotificationError("Ошибка загрузки обновления");

                if (_checkUpdateJobDetail != null)
                {
                    await Scheduler.ResumeJob(_checkUpdateJobDetail.Key);
                }
            }
        }

        private void OnUpdateCurrencyRates(UpdateCurrencyRatesMessage message)
        {
            CurrencyTypeRateDto currency1 = message.CurrencyTypeRates.First(x => x.FromCurrencyTypeId == CurrencyTypeIds.UsdMinusId);
            CurrencyTypeRateDto currency2 = message.CurrencyTypeRates.First(x => x.FromCurrencyTypeId == CurrencyTypeIds.UsdPlusId);

            CultureInfo culture = CultureInfo.InvariantCulture;

            CurrencyReatesText = string.Join("/", currency1.ConversionRate.ToString("F2", culture), currency2.ConversionRate.ToString("F2", culture));
        }

        private void OnUpdateWorkPlace(UpdateWorkPlaceMessage message)
        {
            string workPlaceTypeName = Dictionaries.GetItemById<WorkPlaceType>(message.WorkPlace.TypeId).Name;
            string workPlaceDeviceTypeName = Dictionaries.GetItemById<WorkPlaceDeviceType>(message.WorkPlace.DeviceTypeId).Name;

            WorkPlaceText = $"{workPlaceTypeName}/{workPlaceDeviceTypeName}";
        }

        private void OnUpdateUserNotifications(UpdateUserNotificationsMessage message)
        {
            if (message.Important)
            {
                _notificationBlinkTimer.Start();
            }
            else
            {
                _notificationBlinkTimer.Stop();
                UserNotificationBlink = false;
            }

            UserNotificationsExists = message.MessageCount > 0;

            NotificationItemText = GetNotificationItemText(message.MessageCount);

            RaisePropertyChanged(nameof(UserNotificationsExists));
        }

        private void OnShowNewCommentMessage(ShowNewCommentNotificationMessage message)
        {
            MessageFacadeService.ShowNewCommentNotification(message.Caption, message.Content, message.CommentId, WorkspaceViewModel);
        }

        private void OnLockTimeoutChanged(LockTimeoutChangedMessage message)
        {
            if (message.TimeOut > TimeSpan.Zero)
            {
                _lockClientTimer.Interval = message.TimeOut.Value;
                _lockClientTimer.Start();
            }
            else
            {
                _lockClientTimer.Interval = TimeSpan.Zero;
                _lockClientTimer.Stop();
            }
        }

        private void OnTaskMessage(TaskMessage message)
        {
            if (message.MessageType == MessageType.Changed && (message.Entity.StateId == TaskState.Completed.Id || message.Entity.StateId == TaskState.Cancelled.Id))
            {
                AsyncHelper.RunSync(() => Scheduler.TriggerJob(_getUserNotificationsJobDetail.Key));
            }
        }

        private async void OnUserLoggedIn(UserLoggedInMessage message)
        {
            try
            {
                await await Task.Factory.StartNew(
                    async () =>
                    {
                        SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
                        splashScreenManager.ViewModel.Status = "Загрузка данных";
                        splashScreenManager.Show();

                        CacheSettingsDto cacheSetting = await WebClient.ExecuteApiRequestAsync(new QueryCacheSettings());

                        _liteDbConnectionFactory.Password = cacheSetting.CacheStoragePassword;

                        await _syncCacheJob.ExecuteOnceAsync(default);
                        await _syncCacheJob.StartAsync(default);

                        splashScreenManager.Close();
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.FromCurrentSynchronizationContext());

                if (_showsFirstTime)
                {
                    Dictionaries.LoadAsync().ContinueWith(
                            _ => WorkspaceViewModel.ShowWorkPlaceViewCommand.Execute(null),
                            TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    WorkspaceViewModel.ShowWorkPlaceViewCommand.Execute(null);
                }
            }
            catch (Exception e)
            {
                MessageFacadeService.ShowNotificationError("Непредвиденна ошибка");
                Logger.LogError(e, "Failed to set work place");
            }

            WorkspaceViewModel.NavGroups = NavigationMenuBuilder.BuildNavigationMenu(message.Roles, message.Operations).ToObservableCollection();

            Navigate(ModuleType.Workspace);

            if (_showsFirstTime)
            {
                Messenger.Send(new TelewikiHelpMessage("Вики", null, message.IsReload, message.Cookies));

                _showsFirstTime = false;
            }

            AuthenticatedEmployeeRoles = message.Roles.ToReadOnlyObservableCollection();
            AuthenticatedEmployeeOperations = message.Operations.ToReadOnlyObservableCollection();
            AuthenticatedUserName = message.EmployeeName;

            NotifyAllVisible = WebClient.IsOperationAllowed(BusinessOperation.NotifyAllInClient);
            ActiveClientsVisible = WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.TechSupport);
            SettingsVisible = IsUserAuthenticated();

            if (_notificationBlinkTimer == null)
            {
                _notificationBlinkTimer = new DispatcherTimer();

                _notificationBlinkTimer.Tick += OnNotificationBlinkTimerOnTick;
                _notificationBlinkTimer.Interval = TimeSpan.FromSeconds(1);
            }

            if (_lockClientTimer == null)
            {
                _lockClientTimer = new DispatcherTimer();

                _lockClientTimer.Tick += (_, _) =>
                {
                    LockAsync(false);
                };
            }

            try
            {
                EquipmentSettingsInfo settings = await EquipmentSettingsStore.LoadAsync();

                Messenger.Send(new LockTimeoutChangedMessage(settings?.LockTimeout));
            }
            catch
            {
                // ignored
            }

            WorkspaceViewModel.ShowDocuments();

            void OnNotificationBlinkTimerOnTick(object s, EventArgs e)
            {
                UserNotificationBlink = !UserNotificationBlink;
            }

            if (!_equipmentSettingsCheck)
            {
                await EquipmentSettingsCheckAsync();
            }

            Task.Factory.StartNew(async () =>
            {
                Scheduler ??= await SchedulerFactory.GetScheduler();

                if (_checkNewCommentsJobDetail != null)
                {
                    await Scheduler.ResumeJob(_checkNewCommentsJobDetail.Key);
                    await Scheduler.TriggerJob(_checkNewCommentsJobDetail.Key);
                }
                else
                {
                    await ScheduleCheckNewCommentsJobAsync();
                }

                if (_updateAuthenticatedEmployeeJobDetail != null)
                {
                    await Scheduler.ResumeJob(_updateAuthenticatedEmployeeJobDetail.Key);
                    await Scheduler.TriggerJob(_updateAuthenticatedEmployeeJobDetail.Key);
                }
                else
                {
                    await ScheduleUpdateAuthenticatedEmployeeJobAsync();
                }

                if (_getCurrencyRatesJobDetail != null)
                {
                    await Scheduler.ResumeJob(_getCurrencyRatesJobDetail.Key);
                    await Scheduler.TriggerJob(_getCurrencyRatesJobDetail.Key);
                }
                else
                {
                    await ScheduleGetCurrencyRatesJobAsync();
                }

                if (_getUserNotificationsJobDetail != null)
                {
                    await Scheduler.ResumeJob(_getUserNotificationsJobDetail.Key);
                    await Scheduler.TriggerJob(_getUserNotificationsJobDetail.Key);
                }
                else
                {
                    await ScheduleGetUserNotificationsJobAsync();
                }

                await NotificationHubClient.StartAsync();

                HubConnectionState = NotificationHubClient.State;

                await StartTrackCallsAsync();
            });

            _startInitingWithRefreshToken = false;
        }

        private void OnUserCancelLogin(UserCancelLoginMessage obj)
        {
            CloseTrigger = true;
        }

        private Task ScheduleCheckUpdateJobAsync()
        {
            _checkUpdateJobDetail = JobBuilder.Create<CheckUpdateJob>().WithIdentity("CheckUpdate", "update").Build();

            ITrigger checkUpdateTrigger = TriggerBuilder.Create()
                .WithIdentity("CheckUpdateTrigger", "update")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(_updateManagerOptions.CheckUpdateIntervalSeconds).RepeatForever())
                .Build();

            return Scheduler.ScheduleJob(_checkUpdateJobDetail, checkUpdateTrigger);
        }

        private Task ScheduleCheckNewCommentsJobAsync()
        {
            IDictionary<string, object> jobData = new Dictionary<string, object>
            {
                { "DocumentManagerService", WorkspaceViewModel.DocumentManagerService }
            };

            _checkNewCommentsJobDetail = JobBuilder
                .Create<CheckNewCommentsJob>()
                .SetJobData(new JobDataMap(jobData))
                .WithIdentity("CheckNewComments", "comments")
                .Build();

            ITrigger checkNewCommentsTrigger = TriggerBuilder.Create()
                .WithIdentity("CheckNewCommentsTrigger", "comments")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(60).RepeatForever())
                .Build();

            return Scheduler.ScheduleJob(_checkNewCommentsJobDetail, checkNewCommentsTrigger);
        }

        private Task ScheduleGetCurrencyRatesJobAsync()
        {
            string name = nameof(GetCurrencyRatesJob);

            _getCurrencyRatesJobDetail = JobBuilder.Create<GetCurrencyRatesJob>().WithIdentity($"{name}Job").Build();

            ITrigger getCurrencyRatesJobTrigger = TriggerBuilder.Create()
                .WithIdentity($"{name}Trigger")
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
                .StartNow()
                .Build();

            return Scheduler.ScheduleJob(_getCurrencyRatesJobDetail, getCurrencyRatesJobTrigger);
        }

        private Task ScheduleUpdateAuthenticatedEmployeeJobAsync()
        {
            const string name = nameof(UpdateAuthenticatedEmployeeJob);

            _updateAuthenticatedEmployeeJobDetail = JobBuilder
                .Create<UpdateAuthenticatedEmployeeJob>()
                .WithIdentity($"{name}Job")
                .Build();

            ITrigger updateAuthenticatedEmployeeJobTrigger = TriggerBuilder.Create()
                .WithIdentity($"{name}Trigger")
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
                .StartNow()
                .Build();

            return Scheduler.ScheduleJob(_updateAuthenticatedEmployeeJobDetail, updateAuthenticatedEmployeeJobTrigger);
        }

        private Task ScheduleGetUserNotificationsJobAsync()
        {
            const string name = nameof(GetUserNotificationsJob);

            _getUserNotificationsJobDetail = JobBuilder.Create<GetUserNotificationsJob>().WithIdentity($"{name}Job").Build();

            ITrigger getUserNotificationsTrigger = TriggerBuilder.Create()
                .WithIdentity($"{name}Trigger")
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
                .StartNow()
                .Build();

            return Scheduler.ScheduleJob(_getUserNotificationsJobDetail, getUserNotificationsTrigger);
        }

        private async Task ScheduleShowPhoneHistoryJobAsync()
        {
            if (_showPhoneHistoryJobDetail != null)
            {
                await Scheduler.ResumeJob(_showPhoneHistoryJobDetail.Key);
                await Scheduler.TriggerJob(_showPhoneHistoryJobDetail.Key);
            }
            else if (AuthenticatedEmployeeOperations.Contains(BusinessOperation.CheckOktellState))
            {
                const string name = nameof(ShowPhoneHistoryJob);

                _showPhoneHistoryJobDetail = JobBuilder.Create<ShowPhoneHistoryJob>().WithIdentity($"{name}Job").Build();

                ITrigger showPhoneHistoryTrigger = TriggerBuilder.Create()
                    .WithIdentity($"{name}Trigger")
                    .UsingJobData(new JobDataMap { { ShowPhoneHistoryJob.ConnectedStateIdentity,  null } })
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
                    .StartNow()
                    .Build();

                await Scheduler.ScheduleJob(_showPhoneHistoryJobDetail, showPhoneHistoryTrigger);
            }
        }

        private async Task StartTrackCallsAsync()
        {
            switch (_callTrackOptions.Type)
            {
                case CallTrackType.Oktell:
                    await ScheduleShowPhoneHistoryJobAsync();
                    break;

                case CallTrackType.Asterisk:
                    NotificationHubClient.RegisterHandler<CallNotificationDto>(NotificationHub.CallEventMethodName, CallNotificationHandler);
                    break;
            }

            async Task CallNotificationHandler(CallNotificationDto dto)
            {
                await Mediator.Publish(Mapper.Map<CallNotificationRequest>(dto));
            }
        }

        private async Task StopTrackCallsAsync()
        {
            if (_showPhoneHistoryJobDetail != null)
            {
                await Scheduler.DeleteJob(_showPhoneHistoryJobDetail.Key);
                _showPhoneHistoryJobDetail = null;
            }

            NotificationHubClient.RemoveAllHandlersForMethod(NotificationHub.CallEventMethodName);
        }

        private void ShowUserInfo()
        {
            int? employeeId = WebClient.AuthenticatedEmployee?.Id;

            if (employeeId.HasValue)
            {
                Messenger.Send(new EmployeeViewMessage(employeeId.Value));
            }
        }

        private void UpdateCurrencyRates()
        {
            int? employeeId = WebClient.AuthenticatedEmployee?.Id;

            if (employeeId.HasValue)
            {
                Messenger.Send(new UpdateCurrencyRatesViewMessage());
            }
        }

        private void ShowWorkPlace()
        {
            Messenger.Send(new WorkPlaceViewMessage());
        }

        private void OnNetworkStateChanged()
        {
            DownloadSpeedNetworkStateString = _networkDiagnoser.DownloadSpeedNetworkState switch
            {
                NetworkState.No => "◽️◽️◽️◽️◽️",
                NetworkState.Terrible => "◾️◽️◽️◽️◽️",
                NetworkState.Bad => "◾️◾️◽️◽️◽️",
                NetworkState.Normal => "◾️◾️◾️◽️◽️",
                NetworkState.Good => "◾️◾️◾️◾️◽️",
                NetworkState.Excellent => "◾️◾️◾️◾️◾️",
                _ => "◽️◽️◽️◽️◽️"
            };

            DownloadSpeedNetworkStateToolTip = _networkDiagnoser.ToolTip;
        }

        private void SubscribeOnHubReconnections()
        {
            NotificationHubClient.OnReconnected(_ =>
            {
                HubConnectionState = NotificationHubClient.State;
                return Task.CompletedTask;
            });

            NotificationHubClient.OnConnectionClosed(_ =>
            {
                HubConnectionState = NotificationHubClient.State;
                return Task.CompletedTask;
            });

            NotificationHubClient.OnReconnecting(_ =>
            {
                _yellowConnectionSetTime = DateTime.Now;
                HubConnectionState = NotificationHubClient.State;

                Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
                {
                    if (_yellowConnectionSetTime != null
                        && _yellowConnectionSetTime.Value.AddSeconds(60) <= DateTime.Now
                        && HubConnectionState != HubConnectionState.Connected)
                    {
                        HubConnectionState = HubConnectionState.Disconnected;
                    }
                });

                return Task.CompletedTask;
            });
        }

        private async Task EquipmentSettingsCheckAsync()
        {
            try
            {
                _equipmentSettingsCheck = true;

                IReadOnlyCollection<ValidationResultItem> validationResultItems = await EquipmentSettingsWorker.EquipmentSettingsWorkAsync();

                if (validationResultItems.Any(x => x.IsError))
                {
                    string errors = string.Join($"{Environment.NewLine}", validationResultItems.Select(x => x.Message).ToArray());

                    Logger.LogWarning("Failed checking hardware settings: {Errors}", errors);

                    MessageFacadeService.ShowValidationResultView(
                        "Ошибки настроек оборудования",
                        validationResultItems.Where(x => x.IsError).ToArray(),
                        WorkspaceViewModel);
                }
            }
            catch (Exception e)
            {
                MessageFacadeService.ShowNotificationError("Ошибка проверки настроек оборудования");
                Logger.LogError(e, "Unexpected error checking hardware settings");
            }
        }
    }
}