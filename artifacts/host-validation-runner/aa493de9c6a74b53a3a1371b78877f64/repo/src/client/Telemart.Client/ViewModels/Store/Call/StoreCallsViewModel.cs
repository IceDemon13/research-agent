using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Asterisk;
using Telemart.Client.Common.Layouts;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Call.Actions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class StoreCallsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private int groupId = -1;

        public StoreCallsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IFilterModuleLayoutService<CallFilteringItem> filterModuleLayoutService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(AddCall);
            AddFromOrderCommand = new AsyncCommand(AddFromOrderAsync);
            AddFromServiceRequestCommand = new AsyncCommand(AddFromServiceRequestAsync);
            EditCommand = new DelegateCommand<CallViewItem>(EditCall, x => x != null && !x.IsFolder);
            CancelCallCommand = new AsyncCommand<CallViewItem>(CancelCallAsync, x => x != null && !x.IsFolder);
            CallCommand = new DelegateCommand<CallViewItem>(Call, x => x != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            ChangeCallStateCommand = new DelegateCommand(ChangeCallState, CanChangeCallState);
            OpenOperatorsCommand = new DelegateCommand(OpenOperators);

            Filter = new StoreCallsFilterViewModel(WebClient, Dictionaries);
            Messenger.Register<CallMessage>(this, OnCallMessage);

            Calls = new ObservableRangeCollection<CallViewItem>();

            FilterModuleLayoutService = filterModuleLayoutService;
        }

        public StoreCallsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand CancelCallCommand { get; }

        public IDelegateCommand CallCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IAsyncCommand AddFromOrderCommand { get; }

        public IAsyncCommand AddFromServiceRequestCommand { get; }

        public IDelegateCommand ChangeCallStateCommand { get; }

        public IDelegateCommand OpenOperatorsCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<CallViewItem> Calls
        {
            get { return GetProperty(() => Calls); }
            private set { SetProperty(() => Calls, value, () => RaisePropertiesChanged(nameof(PhonesCount), nameof(CallsCount))); }
        }

        public CallViewItem CurrentCall
        {
            get { return GetProperty(() => CurrentCall); }
            set { SetProperty(() => CurrentCall, value); }
        }

        public List<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public List<EmployeeDto> AsteriskEmployees
        {
            get { return GetProperty(() => AsteriskEmployees); }
            private set { SetProperty(() => AsteriskEmployees, value); }
        }

        public List<CallTypeDto> CallTypes
        {
            get { return GetProperty(() => CallTypes); }
            private set { SetProperty(() => CallTypes, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public StoreCallsFilterViewModel Filter
        {
            get { return GetProperty(() => Filter); }
            private set { SetProperty(() => Filter, value); }
        }

        public int PhonesCount => Calls?.Count(x => x.ParrentId == null) ?? 0;

        public int CallsCount => Calls?.Count(x => !x.IsFolder) ?? 0;

        public IFilterModuleLayoutService<CallFilteringItem> FilterModuleLayoutService { get; }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            if (hotkeyMessage.ModifierKeys == ModifierKeys.Alt)
            {
                switch (hotkeyMessage.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    {
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F2:
                    {
                        EditCommand.Execute(CurrentCall);
                        handled = true;
                        break;
                    }

                case Key.F5:
                    {
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F6:
                    {
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                    }

                case Key.F9:
                    {
                        CallCommand.Execute(CurrentCall);
                        handled = true;
                        break;
                    }
            }

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            RefreshCommand.Execute(null);

            FilterModuleLayoutService.Init(Module.CallsId, this, Filter);
            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            try
            {
                CallTypes = await WebClient.ExecuteApiRequestAsync(new QueryCallTypes(), true).GetPagedResultDataAsync();
                Employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
                AsteriskEmployees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(AsteriskConstants.AsteriskAccount, null)).GetPagedResultDataAsync();

                await Filter.RefreshAsync();

                CallFilteringItem filteringItem = Filter.GetCallFilteringItem();

                PagedResult<CallDto> calls = await WebClient.ExecuteApiRequestAsync(new QueryCalls(filteringItem));

                List<CallViewItem> callViewItems = calls.Data.Select(x => Mapper.Map<CallViewItem>(x)).ToList();

                Calls = null;
                Calls = GroupCalls(callViewItems).ToObservableRangeCollection();

                RaiseProperties();
            }
            catch (UnexpectedSatusException exception)
            {
                Logger.LogError(exception, "Failed to refresh StoreCallsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to refresh StoreCallsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh StoreCallsViewModel");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private IEnumerable<CallViewItem> GroupCalls(ICollection<CallViewItem> calls)
        {
            foreach (IGrouping<string, CallViewItem> grouping in calls.GroupBy(x => x.Phone).Where(x => x.Count() > 1))
            {
                CallViewItem groupCall = CreateGroup(grouping);
                calls.Add(groupCall);
                grouping.ForEach(x => x.ParrentId = groupCall.Id);
            }

            return calls;
        }

        private CallViewItem CreateGroup(IEnumerable<CallViewItem> calls)
        {
            return MapToParent(calls, new CallViewItem { Id = groupId--, IsFolder = true });
        }

        private CallViewItem MapToParent(IEnumerable<CallViewItem> children, CallViewItem parent)
        {
            parent.Subdivision = children.AllEqualOrDefault((x, y) => x.Subdivision.Id == y.Subdivision.Id)?.Subdivision;
            parent.Contractor = children.AllEqualOrDefault((x, y) => x.Contractor?.Id == y.Contractor?.Id)?.Contractor;
            parent.CallType = children.AllEqualOrDefault((x, y) => x.CallType.Id == y.CallType.Id)?.CallType;
            parent.Priority = Dictionaries.GetItemById<Priority>(children.Select(x => x.Priority).Max(x => x.Id));
            parent.Fio = children.AllEqualOrDefault((x, y) => x.Fio == y.Fio)?.Fio;
            parent.IsCompleted = children.AllEqualOrDefault((x, y) => x.IsCompleted == y.IsCompleted)?.IsCompleted ?? false;
            parent.Phone = children.AllEqualOrDefault((x, y) => x.Phone == y.Phone)?.Phone;
            parent.CallFrom = children.AllEqualOrDefault((x, y) => x.CallFrom == y.CallFrom)?.CallFrom;
            parent.CallTo = children.AllEqualOrDefault((x, y) => x.CallTo == y.CallTo)?.CallTo;
            parent.CallState = children.AllEqualOrDefault((x, y) => x.CallState.Id == y.CallState.Id)?.CallState;
            parent.EmployeeCreatedById = children.AllEqualOrDefault((x, y) => x.EmployeeCreatedById == y.EmployeeCreatedById)?.EmployeeCreatedById;
            parent.EmployeeRespId = children.AllEqualOrDefault((x, y) => x.EmployeeRespId == y.EmployeeRespId)?.EmployeeRespId;
            parent.EmployeeCompletedById = children.AllEqualOrDefault((x, y) => x.EmployeeCompletedById == y.EmployeeCompletedById)?.EmployeeCompletedById;

            CallViewItem firstLocked = children.FirstOrDefault(x => x.EmployeeLockId.HasValue);

            parent.EmployeeLockId = firstLocked?.EmployeeLockId;
            parent.EmployeeLockName = firstLocked?.EmployeeLockName;

            return parent;
        }

        private void RaiseProperties()
        {
            RaisePropertyChanged(nameof(PhonesCount));
        }

        private async Task AddFromDocumentAsync(string caption, string title, Func<string, Task<CreateCallParameter>> parameterFunc)
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                caption,
                title,
                @"^\d+$",
                "Номер документа должен быть числом");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                CreateCallParameter parameter = await parameterFunc(fromUserViewModel.Content);

                if (parameter != null)
                {
                    CreateCallViewModel createCallViewModel = DialogDocumentManagerService.ShowView<CreateCallViewModel>(parameter, this);

                    HandleAdded(createCallViewModel.ResultCall);
                }
            }
        }

        private Task AddFromOrderAsync()
        {
            return AddFromDocumentAsync("Введите номер заказа", "Номер заказа", GetOrderParameterAsync);
        }

        private Task AddFromServiceRequestAsync()
        {
            return AddFromDocumentAsync("Введите номер заявки", "Номер сервисной заявки", GetServiceRequestParameterAsync);
        }

        private async Task<CreateCallParameter> GetOrderParameterAsync(string userInput)
        {
            CreateCallParameter createCallParameter = null;

            if (int.TryParse(userInput, out int orderId))
            {
                try
                {
                    OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

                    createCallParameter = new CreateCallParameter(
                        CallDocumentType.Order,
                        order.Id,
                        order.BasedOnServiceRequestId,
                        order.SubdivisionId,
                        order.ClientId,
                        order.Fio,
                        order.Phone,
                        order.Phone2);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{orderId} не найден");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating call from order");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning($"№{userInput} - Неверный формат номера документа");
            }

            return createCallParameter;
        }

        private async Task<CreateCallParameter> GetServiceRequestParameterAsync(string userInput)
        {
            CreateCallParameter createCallParameter = null;
            int serviceRequestId;

            if (int.TryParse(userInput, out serviceRequestId))
            {
                try
                {
                    ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId));

                    createCallParameter = new CreateCallParameter(
                        CallDocumentType.ServiceRequest,
                        serviceRequest.OrderId,
                        serviceRequest.Id,
                        serviceRequest.SubdivisionId,
                        serviceRequest.ContractorId,
                        serviceRequest.Fio,
                        serviceRequest.Phone,
                        serviceRequest.Phone2);
                }
                catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    MessageFacadeService.ShowNotificationWarning($"Сервисная заявка №{serviceRequestId} не найдена");
                }
                catch (Exception exception)
                {
                    MessageFacadeService.ShowNotificationWarning(Resources.ErrorDuringDataLoading);
                    Logger.LogError(exception, "Error while creating call from service request");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning($"№{userInput} - Неверный формат номера документа");
            }

            return createCallParameter;
        }

        private void AddCall()
        {
            CreateCallViewModel viewModel = DialogDocumentManagerService.ShowView<CreateCallViewModel>(CreateCallParameter.Empty, this);
            HandleAdded(viewModel.ResultCall);
        }

        private void HandleAdded(CallDto newCall)
        {
            if (newCall == null)
            {
                return;
            }

            CallViewItem existingCall = Calls.FirstOrDefault(x => x.Id == newCall.Id);

            if (existingCall != null)
            {
                Mapper.Map(newCall, existingCall);
                MessageFacadeService.ShowNotificationInfo($"Задача обновлена для звонка №{newCall.Id}");
                return;
            }

            MessageFacadeService.ShowNotificationInfo($"Звонок №{newCall.Id} успешно создан");

            CallViewItem callViewItem = Mapper.Map<CallViewItem>(newCall);

            Calls.Insert(0, callViewItem);
        }

        private void EditCall(CallViewItem viewItem)
        {
            Messenger.Send(new CallViewMessage(viewItem.Id));
        }

        // TODO: remove duplicates with OrderViewModel, ServiceRequestViewModel
        private async Task CancelCallAsync(CallViewItem callViewItem)
        {
            if (callViewItem.CallState != CallState.New)
            {
                MessageFacadeService.ShowNotificationWarning("Отменять можно только новые звонки");
                return;
            }

            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Причина отмены",
                "Причина");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (fromUserViewModel.IsOk)
            {
                if (string.IsNullOrWhiteSpace(fromUserViewModel.Content))
                {
                    MessageFacadeService.ShowNotificationWarning("Не заполнена причина отмены");
                    return;
                }
            }
            else
            {
                return;
            }

            try
            {
                Result<CallDto> result = await WebClient.ExecuteApiRequestAsync(new CancelCall(callViewItem.Id, fromUserViewModel.Content.Trim()));

                MessageFacadeService.ShowNotificationInfo($"Звонок №{result.Data.Id} успешно отменен");

                Messenger.Send(new CallMessage(result.Data, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Ошибки при отмене звонка", exception.GetErrorItems()),
                    this);
            }
            catch (UnexpectedErrorException)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter(
                        "Ошибки при получении данных",
                        new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }),
                    this);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving call");
                MessageFacadeService.ShowNotificationError("Ошибка при отмене звонка");
            }
        }

        private void Call(CallViewItem viewItem)
        {
            OutcomingCallViewMessage message = viewItem.IsFolder
                ? new OutcomingCallViewMessage(Calls.Where(x => x.ParrentId == viewItem.Id).ToArray())
                : new OutcomingCallViewMessage(viewItem);

            Messenger.Send(message);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void OnCallMessage(CallMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Calls.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem =>
                        {
                            string originalPhone = viewItem.Phone;

                            Mapper.Map(message.Entity, viewItem);

                            if (!string.Equals(originalPhone, viewItem.Phone, StringComparison.OrdinalIgnoreCase))
                            {
                                CallViewItem[] originalPhoneCalls = Calls.Where(x => x.Phone == originalPhone && !x.IsFolder).ToArray();

                                if (originalPhoneCalls.Length == 1 && originalPhoneCalls[0].ParrentId.HasValue)
                                {
                                    int? parrentId = originalPhoneCalls[0].ParrentId;
                                    Calls.RemoveRange(Calls.Where(x => x.Id == parrentId));
                                    originalPhoneCalls[0].ParrentId = null;
                                }
                                else
                                {
                                    CallViewItem parent = Calls.FirstOrDefault(x => x.Id == originalPhoneCalls[0].ParrentId);

                                    if (parent != null)
                                    {
                                        MapToParent(originalPhoneCalls, parent);
                                    }
                                }
                            }

                            ProcessCallMessage(viewItem);
                        });

                        break;
                    }

                case MessageType.Added:
                    {
                        CallViewItem currentCall = Mapper.Map<CallViewItem>(message.Entity);
                        Calls.Add(currentCall);
                        ProcessCallMessage(currentCall);
                        break;
                    }
            }

            void ProcessCallMessage(CallViewItem affectedCall)
            {
                List<CallViewItem> calls = Calls.Where(x => x.Phone == affectedCall.Phone && !x.IsFolder).ToList();

                if (calls.Count > 1)
                {
                    int? parrentId = calls.First(x => x.Id != affectedCall.Id).ParrentId;

                    if (parrentId.HasValue)
                    {
                        CallViewItem groupCall = Calls.First(x => x.Id == parrentId);
                        MapToParent(calls, groupCall);
                        affectedCall.ParrentId = groupCall.Id;
                    }
                    else
                    {
                        CallViewItem groupCall = CreateGroup(calls);
                        Calls.Add(groupCall);
                        calls.ForEach(x => x.ParrentId = groupCall.Id);
                    }
                }
                else
                {
                    affectedCall.ParrentId = null;
                }

                RaiseProperties();
            }
        }

        private void ChangeCallState()
        {
            DialogDocumentManagerService.ShowView<ChangeCallStateViewModel>(null, this);
        }

        private void OpenOperators()
        {
            SizeableDialogDocumentManagerService.ShowView<OperatorsViewModel>(null, this);
        }

        private bool CanChangeCallState()
        {
            return AsteriskEmployees?.Any(x => x.Id == WebClient.AuthenticatedEmployee.Id) == true;
        }
    }
}