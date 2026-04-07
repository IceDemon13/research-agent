using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.HubFactory;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.HubClient.Base;
using Telemart.Client.Data.HubClient.Hubs;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Complaint;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Stores;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.Factories.Complaint;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Dialogs.Call;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class OutcomingCallViewModel : TelemartDialogViewModelBase
    {
        private const int MaxAttemptsCount = 5;

        private List<EmployeeDto> employees;
        private List<ContractorDto> contractors;
        private CallViewItem modelOriginal;

        public OutcomingCallViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IHubClientFactory hubClientFactory,
            OrderComplaintCreator orderComplaintCreator,
            EmptyComplaintCreator emptyComplaintCreator,
            ServiceRequestComplaintCreator serviceRequestComplaintCreator,
            TradeInComplaintCreator tradeInComplaintCreator,
            DocumentCommands documentCommands,
            IErrorHandler errorHandler,
            ICallStore callStore)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            DocumentCommands = documentCommands;
            Mapper = mapper;
            CallStore = callStore;
            ErrorHandler = errorHandler;

            CallHubClient = hubClientFactory.Create<CallHub>(messageFacadeService);
            CallHubClient.RegisterHandler<CallDependencyDto>(CallHub.GetCallDependencyMethod, OnCallDependencyAsync);

            Messenger.Register<CallDependencyMessage>(this, OnCallDependencyChanged);
            Messenger.Register<OnOrderBeforeEditMessage>(this, _ => IsLongOperation = false);

            DeleteDependencyCommand = new DelegateCommand(DeleteDependency, () => SelectedDependency is not null && !SelectedDependency.System);
            OpenOrderCommand = new DelegateCommand<int?>(OpenOrder, x => x.HasValue);
            OpenServiceRequestCommand = new DelegateCommand<int?>(OpenServiceRequest, x => x.HasValue);
            HandleCallStateChangedCommand = new AsyncCommand(HandleCallStateChangedAsync);
            HandleCallLaterChangedCommand = new AsyncCommand(HandleCallLaterChangedAsync);
            NavigateCreatedFrom = new DelegateCommand(() => ProcessHelper.Start(Model.CreatedFrom), () => !string.IsNullOrWhiteSpace(Model.CreatedFrom));
            LinkOrderCommand = new AsyncCommand(LinkOrderAsync);
            LinkServiceRequestCommand = new AsyncCommand(LinkServiceRequestAsync);
            LinkComplaintCommand = new AsyncCommand(LinkComplaintAsync);
            CreateOrderCommand = new DelegateCommand(CreateOrder);
            CreateServiceRequestCommand = new DelegateCommand(CreateServiceRequest);
            ShowCallDialogCommand = new DelegateCommand<CallDialogParameter>(ShowCallDialog, x => x != null && Model.CallState == CallState.New);
            HandleRowDoubleClickCommand = new AsyncCommand<RowDoubleClickEventArgs>(HandleRowDoubleClickAsync);
            CreateUnpackEventCommand = new AsyncCommand(CreateUnpackEventAsync, () => OrderId != null);

            CreateComplaintByOrderCommand = new AsyncCommand(() => orderComplaintCreator.ShowViewAsync(DialogDocumentManagerService, NonModalDialogDocumentManagerService, this));
            CreateComplaintByServiceRequestCommand = new AsyncCommand(() => serviceRequestComplaintCreator.ShowViewAsync(DialogDocumentManagerService, NonModalDialogDocumentManagerService, this));
            CreateComplaintByTradeInCommand = new AsyncCommand(() => tradeInComplaintCreator.ShowViewAsync(DialogDocumentManagerService, NonModalDialogDocumentManagerService, this));
            CreateComplaintCommand = new AsyncCommand(() => emptyComplaintCreator.ShowViewAsync(DialogDocumentManagerService, NonModalDialogDocumentManagerService, this));
        }

        public OutcomingCallViewModel()
        {
        }

        #region Commands

        public IDelegateCommand OpenOrderCommand { get; }

        public IDelegateCommand ShowCallDialogCommand { get; }

        public IDelegateCommand OpenServiceRequestCommand { get; }

        public IAsyncCommand HandleCallLaterChangedCommand { get; }

        public IAsyncCommand HandleCallStateChangedCommand { get; }

        public IDelegateCommand DeleteDependencyCommand { get; }

        public IDelegateCommand CreateOrderCommand { get; }

        public IDelegateCommand CreateServiceRequestCommand { get; }

        public IAsyncCommand CreateComplaintByTradeInCommand { get; }

        public IAsyncCommand CreateComplaintByServiceRequestCommand { get; }

        public IAsyncCommand CreateComplaintByOrderCommand { get; }

        public IAsyncCommand CreateComplaintCommand { get; }

        public IAsyncCommand LinkOrderCommand { get; }

        public IAsyncCommand LinkServiceRequestCommand { get; }

        public IAsyncCommand LinkComplaintCommand { get; }

        public IDelegateCommand NavigateCreatedFrom { get; }

        public DocumentCommands DocumentCommands { get; }

        public IAsyncCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand CreateUnpackEventCommand { get; }

        #endregion

        #region INPC

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            private set { SetProperty(() => OrderId, value); }
        }

        public int? ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            private set { SetProperty(() => ServiceRequestId, value); }
        }

        public DateTime? NewCallFrom
        {
            get { return GetProperty(() => NewCallFrom); }
            set { SetProperty(() => NewCallFrom, value); }
        }

        public DateTime? NewCallTo
        {
            get { return GetProperty(() => NewCallTo); }
            set { SetProperty(() => NewCallTo, value); }
        }

        public string NewTaskText
        {
            get { return GetProperty(() => NewTaskText); }
            set { SetProperty(() => NewTaskText, value); }
        }

        public bool? Resolved
        {
            get { return GetProperty(() => Resolved); }
            set { SetProperty(() => Resolved, value, ResultChanged); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public string TaskResultText
        {
            get { return GetProperty(() => TaskResultText); }
            set { SetProperty(() => TaskResultText, value); }
        }

        public CallState CallState
        {
            get { return GetProperty(() => CallState); }
            set { SetProperty(() => CallState, value); }
        }

        public ObservableCollection<CallState> CallStates
        {
            get { return GetProperty(() => CallStates); }
            set { SetProperty(() => CallStates, value); }
        }

        public bool CallLater
        {
            get { return GetProperty(() => CallLater); }
            set { SetProperty(() => CallLater, value); }
        }

        public ReadOnlyObservableCollection<CallDependencyType> DependencyTypes
        {
            get { return GetProperty(() => DependencyTypes); }
            set { SetProperty(() => DependencyTypes, value); }
        }

        public CallDependencyViewItem SelectedDependency
        {
            get { return GetProperty(() => SelectedDependency); }
            set { SetProperty(() => SelectedDependency, value); }
        }

        public CallViewItem Model
        {
            get { return GetProperty(() => Model); }
            set { SetProperty(() => Model, value); }
        }

        #endregion

        public double Top
        {
            get { return GetProperty(() => Top); }
            set { SetProperty(() => Top, value); }
        }

        public double Left
        {
            get { return GetProperty(() => Left); }
            set { SetProperty(() => Left, value); }
        }

        public new int Width
        {
            get { return GetProperty(() => Width); }
            set { SetProperty(() => Width, value); }
        }

        public new int Height
        {
            get { return GetProperty(() => Height); }
            set { SetProperty(() => Height, value); }
        }

        public bool ReadOnly
        {
            get { return GetProperty(() => ReadOnly); }
            set { SetProperty(() => ReadOnly, value); }
        }

        public bool IsLongOperation
        {
            get { return GetProperty(() => IsLongOperation); }
            set { SetProperty(() => IsLongOperation, value); }
        }

        public bool IsEnabledOk
        {
            get { return GetProperty(() => IsEnabledOk); }
            set { SetProperty(() => IsEnabledOk, value); }
        }

        public bool IsChanged => CompareHelper.IsChanged();

        public bool CanCreateUnpackEvent => WebClient.IsOperationAllowed(BusinessOperation.OrderUnpackEvent);

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IHubClientBase<CallHub> CallHubClient { get; }

        private ICallStore CallStore { get; }

        private TelemartCompareHelper<CallViewItem> CompareHelper => new TelemartCompareHelper<CallViewItem>(modelOriginal, Model, GetMembersToIgnore());

        public static void BuildMetadata(MetadataBuilder<OutcomingCallViewModel> builder)
        {
            builder.Property(x => x.CallState)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TaskResultText)
                .MaxLength(255);

            builder.Property(x => x.NewTaskText)
                .MaxLength(255)
                .MatchesInstanceRule(
                    (x, y) => y.CallState != CallState.NotSolved || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.NewCallFrom)
                .MatchesInstanceRule(
                    (x, y) =>
                    (y.CallState != CallState.NotSolved && (y.CallState != CallState.NotReached || !y.CallLater)) || x.HasValue,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.NewCallTo)
                .MatchesInstanceRule(
                    (x, y) => (y.CallState != CallState.NotSolved && (y.CallState != CallState.NotReached || !y.CallLater)) || x.HasValue,
                    () => Resources.RequiredErrorMessage);
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && IsChanged && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                CallHubClient.Dispose();

                if (CallStore.CallId == Model.Id)
                {
                    CallStore.Clear();
                }

                Messenger.Send(new CallFinishMessage());

                base.OnClose(e);
            }
        }

        public override void OnDestroy()
        {
            try
            {
                LockResponse<CallDto> response = WebClient.ExecuteApiRequest(new UnlockCall(Model.Id));

                Messenger.Send(new CallMessage(response.Dto, MessageType.Changed));

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationError("Не удалось разблокировать документ");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unlock entity");
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании");
            }

            base.OnDestroy();
        }

        protected override void OnInitializeInDesignMode()
        {
            DependencyTypes = new[]
            {
                new CallDependencyType(1, "Товар в заказе")
            }.ToReadOnlyObservableCollection();

            Model = new CallViewItem()
            {
                Dependencies = new ObservableCollection<CallDependencyViewItem>()
                {
                    new CallDependencyViewItem()
                    {
                        DependencyTypeId = 1
                    }
                }
            };

            base.OnInitializeInDesignMode();
        }

        protected override void HandleCancel()
        {
            if (CallStore.CallId == Model.Id)
            {
                CallStore.Clear();
            }

            base.HandleCancel();
        }

        protected override async Task HandleLoadedAsync()
        {
            object[] parameters = (object[])Parameter;

            Model = (CallViewItem)parameters[0];
            int windowNumber = (int)parameters[1];
            int windowsCount = (int)parameters[2];

            ReadOnly = Model.CallState.Id != CallState.New.Id;

            if (ReadOnly)
            {
                CallState = Model.CallState;
                TaskResultText = Model.Result;
            }

            IsEnabledOk = Model?.CallState == CallState.New;

            if (windowsCount > 1)
            {
                SetWindowPosition(windowNumber);
            }
            else
            {
                SetWindowCenterPosition();
            }

            CallStates = Dictionaries.GetItems<CallState>().Where(x => x.Id != CallState.New.Id && x.Id != CallState.Canceled.Id).ToObservableCollection();
            employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            DependencyTypes = Dictionaries.GetItems<CallDependencyType>().ToReadOnlyObservableCollection();

            SummaryItems = GetSummaryItems(Model);

            OrderId = Model.OrderId;
            ServiceRequestId = Model.ServiceRequestId;

            Model.Dependencies ??= new ObservableCollection<CallDependencyViewItem>();

            await CallHubClient.StartAsync();

            Title = $"Звонок №{Model.Id}";

            modelOriginal = Model.Clone();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (CallState == null)
            {
                MessageFacadeService.ShowNotificationWarning("Заполните результат");
                return;
            }

            if (NewCallFrom > NewCallTo)
            {
                MessageFacadeService.ShowNotificationWarning("Дата начала звонка должна быть меньше, чем дата окончания");
                return;
            }

            string errorMessage = "Ошибка при сохранении звонка";

            try
            {
                CompleteCall request = GetMakeCallRequest();

                Result<CallDto> result = await WebClient.ExecuteApiRequestAsync(request);

                Messenger.Send(new CallMessage(result.Data, MessageType.Changed));

                MessageFacadeService.ShowNotificationInfo($"Звонок №{result.Data.Id} успешно сохранен");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(errorMessage);
                ShowValidationResultView(errorMessage, exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(errorMessage);
                ShowValidationResultView(errorMessage, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError(errorMessage);
            }
        }

        private void SetWindowPosition(int windowNumber)
        {
            const double Margin = 5;

            Rect parentControlRect = new Rect(Margin, Margin, SystemParameters.PrimaryScreenWidth - Margin, SystemParameters.PrimaryScreenHeight - Margin);

            Top = parentControlRect.Top + (parentControlRect.Height / 2) - ((double)Height / 2);
            Left = parentControlRect.Left + (parentControlRect.Width / 2) - ((double)Width / 2);

            int countWindowsInLine = (int)(parentControlRect.Width / Width);
            int countWindowsLines = (int)(parentControlRect.Height / Height);

            if (countWindowsInLine > 0)
            {
                int lineNumber = windowNumber / countWindowsInLine;

                int colNumber = windowNumber % countWindowsInLine;

                if (colNumber > 0)
                {
                    lineNumber++;
                }
                else
                {
                    colNumber = countWindowsInLine;
                }

                double height = Height * lineNumber;

                if (height < parentControlRect.Height)
                {
                    Top = height - Height + parentControlRect.Top;
                    Left = (Width * (colNumber - 1)) + parentControlRect.Left;
                }
            }

            int maxWindowsCount = countWindowsInLine * countWindowsLines;

            if (windowNumber > maxWindowsCount)
            {
                Top += 20 * (windowNumber - maxWindowsCount);
                Left += 20 * (windowNumber - maxWindowsCount);
            }
        }

        private void DeleteDependency()
        {
            Model.Dependencies.Remove(SelectedDependency);
        }

        private void SetWindowCenterPosition()
        {
            Top = (SystemParameters.PrimaryScreenHeight / 2) - ((double)Height / 2);
            Left = (SystemParameters.PrimaryScreenWidth / 2) - ((double)Width / 2);
        }

        private Task HandleCallStateChangedAsync()
        {
            RaisePropertiesChanged(nameof(TaskResultText), nameof(NewTaskText), nameof(NewCallFrom), nameof(NewCallTo));

            CallLater = CallState == CallState.NotReached
                        && Model.Attempt < MaxAttemptsCount
                        && Model.CallType.ParentId != CallTypeConstants.CallbackCallTypeId;

            return Task.CompletedTask;
        }

        private async Task HandleCallLaterChangedAsync()
        {
            try
            {
                if (CallLater)
                {
                    CallIntervalResponse response = await WebClient.ExecuteApiRequestAsync(new QueryCallInterval(Model.CallType.ParentId, Model.Attempt + 1));

                    NewCallFrom = response.Form;
                    NewCallTo = response.To;
                }
                else
                {
                    NewCallFrom = null;
                    NewCallTo = null;
                }
            }
            catch (Exception)
            {
                NewCallFrom = null;
                NewCallFrom = null;

                MessageFacadeService.ShowNotificationError("Ошибка при вычислении интервала");
            }
        }

        private CompleteCall GetMakeCallRequest()
        {
            bool callLater = false;
            string result = null;
            string task = null;
            DateTime? callFrom = null;
            DateTime? callTo = null;

            CallState state = CallState;

            switch (state.Id)
            {
                case CallState.SolvedId:
                    result = TaskResultText;
                    break;
                case CallState.NotSolvedId:
                    task = NewTaskText;
                    callFrom = NewCallFrom;
                    callTo = NewCallTo;
                    break;
                case CallState.NotReachedId:
                    callLater = CallLater;
                    callFrom = NewCallFrom;
                    callTo = NewCallTo;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new CompleteCall(
                Model.Id,
                state.Id,
                result,
                task,
                callLater,
                callFrom,
                callTo,
                Model.Dependencies.Select(x => new CallDependencyDto()
                {
                    Id = x.Id,
                    DocumentId = x.DocumentId,
                    Name = x.Name,
                    Comment = x.Comment,
                    DependencyTypeId = x.DependencyTypeId,
                    CallId = Model.Id,
                    Approved = x.Approved,
                    System = x.System
                }).ToArray());
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems(CallViewItem viewItem)
        {
            string createdByAndWhen = $"{employees.FirstOrDefault(x => x.Id == viewItem.EmployeeCreatedById)?.Name} ({viewItem.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})";
            string responsible = employees.FirstOrDefault(x => x.Id == viewItem.EmployeeRespId)?.Name;
            string contractor = contractors.FirstOrDefault(x => x.Id == viewItem.ContractorId)?.Name;

            yield return new SummaryViewItem("Подразд.", viewItem.Subdivision.Name);

            if (!string.IsNullOrEmpty(contractor))
            {
                yield return new SummaryViewItem("Контрагент", $"{contractor}");
            }

            yield return new SummaryViewItem("Тип", $"{viewItem.CallType.Parent?.Name} ({viewItem.CallType.Name})");
            yield return new SummaryViewItem("Приоритет", viewItem.Priority.Name);
            yield return new SummaryViewItem("Попытка", viewItem.Attempt.ToString());
            yield return new SummaryViewItem("Создал", createdByAndWhen);

            if (!string.IsNullOrEmpty(responsible))
            {
                yield return new SummaryViewItem("Ответств.", responsible);
            }

            yield return new SummaryViewItem("Звонок", viewItem.CallDisplayTime, viewItem.CallFrom <= DateTime.Now ? SummaryViewItem.GreenLevel : SummaryViewItem.NormalLevel);
            yield return new SummaryViewItem("ФИО", viewItem.Fio);
            yield return new SummaryViewItem("Телефон", viewItem.DisplayPhones);
            yield return new SummaryViewItem("Задача", viewItem.Task);

            if (!string.IsNullOrWhiteSpace(Model.CreatedFrom))
            {
                yield return new SummaryViewItem("Создан с", viewItem.CreatedFrom);
            }
        }

        private async Task OnCallDependencyAsync(CallDependencyDto dto)
        {
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (dto is null || dto.CallId != Model.Id || Model.Dependencies.Any(x =>
                    x.DependencyTypeId == dto.DependencyTypeId
                    && x.DocumentId == dto.DocumentId
                    && x.Name == dto.Name))
                {
                    return;
                }

                Model.Dependencies.Add(Mapper.Map<CallDependencyViewItem>(dto));
            });
        }

        private void OpenOrder(int? orderId)
        {
            if (orderId.HasValue)
            {
                Messenger.Send(new OrderEditViewMessage(orderId.Value));
            }
        }

        private void OpenServiceRequest(int? serviceRequestId)
        {
            if (serviceRequestId.HasValue)
            {
                Messenger.Send(new ServiceRequestViewMessage(serviceRequestId.Value));
            }
        }

        private void CreateOrder()
        {
            Messenger.Send(new OrderCreateViewMessage());
        }

        private void CreateServiceRequest()
        {
            Messenger.Send(new ServiceRequestCreateViewMessage());
        }

        private async Task LinkComplaintAsync()
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter("Номер жалобы", "Введите номер жалобы", "^[0-9]{1,9}$", "Допускаются только целые числа. "), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (!int.TryParse(viewModel.Content, out int complaintId))
            {
                MessageFacadeService.ShowNotificationError("Не удалось преобразовать значение");
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new QueryComplaint(complaintId));
            }
            catch (UnexpectedSatusException)
            {
                MessageFacadeService.ShowNotificationWarning($"Жалоба №{complaintId} не найдена");
                return;
            }

            if (Model.Dependencies.Any(x => x.DependencyTypeId == CallDependencyType.ComplaintId && x.DocumentId == complaintId))
            {
                MessageFacadeService.ShowNotificationWarning($"Жалоба №{complaintId} уже добавлена");
                return;
            }

            Model.Dependencies.Add(new CallDependencyViewItem()
            {
                Id = 0,
                Approved = true,
                System = false,
                DocumentId = complaintId,
                DependencyTypeId = CallDependencyType.ComplaintId,
                Name = "Обсуждение жалобы"
            });
        }

        private async Task LinkServiceRequestAsync()
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter("Номер СЗ", "Введите номер СЗ", "^[0-9]{1,9}$", "Допускаются только целые числа. "), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (!int.TryParse(viewModel.Content, out int serviceRequestId))
            {
                MessageFacadeService.ShowNotificationError("Не удалось преобразовать значение");
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId));
            }
            catch (UnexpectedSatusException)
            {
                MessageFacadeService.ShowNotificationWarning($"Сервисная заявка №{serviceRequestId} не найдена");
                return;
            }

            if (Model.Dependencies.Any(x => x.DependencyTypeId == CallDependencyType.ServiceRequestId && x.DocumentId == serviceRequestId))
            {
                MessageFacadeService.ShowNotificationWarning($"СЗ №{serviceRequestId} уже добавлена");
                return;
            }

            Model.Dependencies.Add(new CallDependencyViewItem()
            {
                Id = 0,
                Approved = true,
                System = false,
                DocumentId = serviceRequestId,
                DependencyTypeId = CallDependencyType.ServiceRequestId,
                Name = "Обсуждение СЗ"
            });
        }

        private async Task LinkOrderAsync()
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                new GetTextFromUserParameter("Номер заказа", "Введите номер заказа", "^[0-9]{1,9}$", "Допускаются только целые числа. "), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (!int.TryParse(viewModel.Content, out int orderId))
            {
                MessageFacadeService.ShowNotificationError("Не удалось преобразовать номер заказа");
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));
            }
            catch (UnexpectedSatusException)
            {
                MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} не найден");
                return;
            }

            if (Model.Dependencies.Any(x => x.DependencyTypeId == CallDependencyType.OrderId && x.DocumentId == orderId))
            {
                MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} уже добавлен");
                return;
            }

            Model.Dependencies.Add(new CallDependencyViewItem()
            {
                Id = 0,
                Approved = true,
                System = false,
                DocumentId = orderId,
                DependencyTypeId = CallDependencyType.OrderId,
                Name = "Обсуждение заказа"
            });
        }

        private void ShowCallDialog(CallDialogParameter parameter)
        {
            CallStore.SetCallId(Model.Id);

            Messenger.Send(new CallFinishMessage());

            DocumentCommands.ShowCallDialogCommand.Execute(parameter);
        }

        private void ResultChanged()
        {
            RaisePropertiesChanged(nameof(TaskResultText), nameof(NewTaskText), nameof(NewCallTo), nameof(NewCallFrom));
        }

        private void OnCallDependencyChanged(CallDependencyMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    if (Model.Dependencies.Any(x =>
                            x.DependencyTypeId == message.DependencyTypeId && x.DocumentId == message.DocumentId && x.Name == message.Name && x.Comment == message.Comment))
                    {
                        MessageFacadeService.ShowNotificationWarning("Действие с таким комментарием уже добавлено");

                        Messenger.Send(new CallDependencyWarningMessage(message.DependencyTypeId, message.Name, message.CallId, message.DocumentId, message.Comment));

                        return;
                    }

                    Model.Dependencies.Add(new CallDependencyViewItem()
                    {
                        Id = 0,
                        Name = message.Name,
                        DependencyTypeId = message.DependencyTypeId,
                        DocumentId = message.DocumentId,
                        Comment = message.Comment,
                        Approved = true,
                        System = false
                    });
                    break;
            }
        }

        private async Task HandleRowDoubleClickAsync(RowDoubleClickEventArgs item)
        {
            if (item.HitInfo.Column.FieldName == nameof(SelectedDependency.DocumentId))
            {
                if (SelectedDependency.DocumentId == null || SelectedDependency.DependencyTypeId == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Документ не доступен");
                    return;
                }

                switch (SelectedDependency.DependencyTypeId)
                {
                    case CallDependencyType.OrderId:
                        IsLongOperation = true;
                        Messenger.Send(new OrderEditViewMessage(SelectedDependency.DocumentId.Value));
                        break;
                    case CallDependencyType.ServiceRequestId:
                        Messenger.Send(new ServiceRequestViewMessage(SelectedDependency.DocumentId.Value));
                        break;
                    case CallDependencyType.ComplaintId:
                        Messenger.Send(new ComplaintViewMessage(SelectedDependency.DocumentId.Value));
                        break;
                    case CallDependencyType.CallId:
                        CallDto call = await WebClient.ExecuteApiRequestAsync(new QueryCall(SelectedDependency.DocumentId.Value));

                        if (call.StateId == CallState.NewId)
                        {
                            CallViewItem callItem = Mapper.Map<CallViewItem>(call);
                            OutcomingCallViewMessage message = new OutcomingCallViewMessage(callItem);
                            Messenger.Send(message);
                        }
                        else
                        {
                            Messenger.Send(new CallViewMessage(call.Id));
                        }

                        break;
                    default:
                        MessageFacadeService.ShowNotificationWarning("Документ не доступен");
                        break;
                }
            }
        }

        private IEnumerable<string> GetMembersToIgnore()
        {
            yield break;
        }

        private async Task CreateUnpackEventAsync()
        {
            Task<OrderDto> orderFromServerTask = GetOrderAsync(OrderId.Value);
            Task<UnpackOrderEventInfoDto> unpackOrderEventInfoTask = GetUnpackOrderEventInfoAsync(OrderId.Value);

            await Task.WhenAll(orderFromServerTask, unpackOrderEventInfoTask);

            OrderDto orderDto = orderFromServerTask.Result;
            UnpackOrderEventInfoDto unpackOrderEventInfoDto = unpackOrderEventInfoTask.Result;

            if (unpackOrderEventInfoDto.Errors?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки", unpackOrderEventInfoDto.Errors.Select(x => new ValidationResultItem(x, true)).ToArray(), this);
                return;
            }

            if (orderDto == null)
            {
                MessageFacadeService.ShowNotificationWarning("Заказ не найден");
                return;
            }

            DialogDocumentManagerService.ShowView<CreateUnpackOrderEventViewModel>(
                new CreateUnpackOrderEventParameter(
                    orderDto.Id,
                    orderDto.WarehouseId ?? 0,
                    unpackOrderEventInfoDto.EventForWarehouseEmployees,
                    unpackOrderEventInfoDto.TaskForPickupEmployees),
                this);
        }

        private async Task<OrderDto> GetOrderAsync(int orderId)
        {
            return await ErrorHandler.HandleErrorsAsync(
                async _ => await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId)).ConfigureAwait(false),
                "получении заказа",
                null,
                this,
                false,
                showNotification: false).ConfigureAwait(false);
        }

        private async Task<UnpackOrderEventInfoDto> GetUnpackOrderEventInfoAsync(int orderId)
        {
            return await ErrorHandler.HandleErrorsAsync(
                async _ => await WebClient.ExecuteApiRequestAsync(new QueryUnpackOrderEventInfo(orderId)).ConfigureAwait(false),
                "получении информации по событию распоковки заказа",
                null,
                this,
                false,
                showNotification: false).ConfigureAwait(false);
        }
    }
}