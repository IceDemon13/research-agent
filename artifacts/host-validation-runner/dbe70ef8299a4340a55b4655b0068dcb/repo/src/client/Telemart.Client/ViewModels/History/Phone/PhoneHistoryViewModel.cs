using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.PhoneHistory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.Requests.Features.History;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Stores;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Complaint;
using Telemart.Client.ViewModels.Customer;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.History.Phone
{
    public class PhoneHistoryViewModel : TelemartDialogViewModelBase
    {
        private List<PhoneHistoryViewItem> allPhoneHistory;
        private IAsyncCommand handleParameterChangedCommand;
        private string[] phones;

        public PhoneHistoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            ICallStore callStore,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            ErrorHandler = errorHandler;
            CallStore = callStore;

            handleParameterChangedCommand = new AsyncCommand<string[]>(HandleParameterChangedAsync, _ => true, true);

            HandleRowDoubleClickCommand = new AsyncCommand<PhoneHistoryViewItem>(HandleRowDoubleClickAsync);
            CreateOrderCommand = new AsyncCommand(CreateOrderAsync);
            CreateServiceRequestCommand = new DelegateCommand<PhoneHistoryViewItem>(CreateServiceRequest);
            EditCancelOrderCommand = new AsyncCommand<PhoneHistoryViewItem>(EditCancelOrderAsync, CanEditCancelOrder);
            EditConfirmOrderCommand = new AsyncCommand<PhoneHistoryViewItem>(EditConfirmOrderAsync, CanConfirmOrder);
            EditOrderCommand = new DelegateCommand<PhoneHistoryViewItem>(EditOrder, CanEditOrder);
            EditPaymentCommand = new AsyncCommand<PhoneHistoryViewItem>(EditPaymentAsync, CanEditPayment);
            EditMergeOrderCommand = new AsyncCommand<PhoneHistoryViewItem>(EditMergeOrderAsync, CanMerge);
            CreateCommentConsultationCommand = new DelegateCommand<PhoneHistoryCommentParameter>(CreateCommentConsultation);
            CreateCommentCommand = new DelegateCommand<PhoneHistoryCommentParameter>(CreateComment);
            CreateComlaintCommand = new AsyncCommand<PhoneHistoryCommentParameter>(CreateComplaintAsync);

            PhoneHistory = new ObservableRangeCollection<PhoneHistoryViewItem>();
            Messenger.Register<OnOrderCreationFinishedMessage>(this, _ => IsCreateOrderButtonEnabled = true);
            Messenger.Register<OnOrderBeforeEditMessage>(this, _ => IsLongOperation = false);
            Messenger.Register<CallDependencyWarningMessage>(this, CallDependencyWarning);
            Messenger.Register<CallFinishMessage>(this, _ => RaisePropertiesChanged(nameof(MenuButtonEnabled), nameof(OrderButtonEnabled), nameof(ServiceRequestButtonEnabled)));
            LockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();
        }

        public IAsyncCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand CreateOrderCommand { get; }

        public IAsyncCommand EditCancelOrderCommand { get; }

        public IAsyncCommand EditConfirmOrderCommand { get; }

        public IDelegateCommand EditOrderCommand { get; }

        public IAsyncCommand EditMergeOrderCommand { get; }

        public IDelegateCommand CreateServiceRequestCommand { get; }

        public IDelegateCommand CreateTradeInCommand { get; }

        public IDelegateCommand CreateCommentCommand { get; }

        public IAsyncCommand EditPaymentCommand { get; }

        public IDelegateCommand CreateCommentConsultationCommand { get; }

        public IAsyncCommand CreateComlaintCommand { get; }

        public override int MinWidth { get; } = 800;

        public override int MinHeight { get; } = 450;

        public override int Width { get; } = 800;

        public override int Height { get; } = 450;

        public ObservableRangeCollection<PhoneHistoryViewItem> PhoneHistory
        {
            get { return GetProperty(() => PhoneHistory); }
            set { SetProperty(() => PhoneHistory, value); }
        }

        public PhoneHistoryViewItem SelectedPhoneHistory
        {
            get { return GetProperty(() => SelectedPhoneHistory); }
            set { SetProperty(() => SelectedPhoneHistory, value, () => RaisePropertiesChanged(nameof(ServiceRequestButtonEnabled), nameof(OrderButtonEnabled), nameof(TradeInButtonEnabled))); }
        }

        public ReadOnlyObservableCollection<PhoneHistoryType> Types
        {
            get { return GetProperty(() => Types); }
            set { SetProperty(() => Types, value); }
        }

        public string PlusHashtags
        {
            get { return GetProperty(() => PlusHashtags); }
            set { SetProperty(() => PlusHashtags, value, () => RaisePropertyChanged(nameof(ShowPlusHashtags))); }
        }

        public string MinusHashtags
        {
            get { return GetProperty(() => MinusHashtags); }
            set { SetProperty(() => MinusHashtags, value, () => RaisePropertyChanged(nameof(ShowMinusHashtags))); }
        }

        public ReadOnlyObservableCollection<string> Phones
        {
            get { return GetProperty(() => Phones); }
            set { SetProperty(() => Phones, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Customers
        {
            get { return GetProperty(() => Customers); }
            set { SetProperty(() => Customers, value); }
        }

        public ObservableCollection<string> SelectedPhones
        {
            get { return GetProperty(() => SelectedPhones); }
            set { SetProperty(() => SelectedPhones, value, RefreshPhoneHistory); }
        }

        public ObservableCollection<int> SelectedCustomers
        {
            get { return GetProperty(() => SelectedCustomers); }
            set { SetProperty(() => SelectedCustomers, value, RefreshPhoneHistory); }
        }

        public bool IsCreateOrderButtonEnabled
        {
            get { return GetProperty(() => IsCreateOrderButtonEnabled); }
            set { SetProperty(() => IsCreateOrderButtonEnabled, value); }
        }

        public bool IsLongOperation
        {
            get { return GetProperty(() => IsLongOperation); }
            set { SetProperty(() => IsLongOperation, value); }
        }

        public bool ShowMinusHashtags => !string.IsNullOrEmpty(MinusHashtags);

        public bool ShowPlusHashtags => !string.IsNullOrEmpty(PlusHashtags);

        public bool MenuButtonEnabled => CallStore.CallId != null;

        public bool OrderButtonEnabled => CallStore.CallId != null && SelectedPhoneHistory != null && SelectedPhoneHistory.TypeId == PhoneHistoryType.OrderId;

        public bool ServiceRequestButtonEnabled => CallStore.CallId != null && SelectedPhoneHistory != null && SelectedPhoneHistory.TypeId == PhoneHistoryType.ServiceRequestId;

        public bool TradeInButtonEnabled => CallStore.CallId != null && SelectedPhoneHistory != null && SelectedPhoneHistory.TypeId == PhoneHistoryType.TradeInId;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private LockableOperationProcessor<OrderDto> LockableOperationProcessor { get; }

        private ICallStore CallStore { get; }

        protected override Task HandleLoadedAsync()
        {
            Title = "Поиск по номеру телефона";
            return Task.CompletedTask;
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            phones = (string[])parameter;

            handleParameterChangedCommand.ExecuteAsync(parameter);

            base.OnParameterChanged(parameter);
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private async Task HandleParameterChangedAsync(string[] _)
        {
            if (phones?.Any(x => !string.IsNullOrWhiteSpace(x)) == true)
            {
                Types = Dictionaries.GetItems<PhoneHistoryType>().ToReadOnlyObservableCollection();

                try
                {
                    PhoneHistoryResultDto result = await WebClient.ExecuteApiRequestAsync(new QueryPhoneHistory(phones));

                    allPhoneHistory = result.History
                        .OrderByDescending(x => x.CreatedOn)
                        .Select(x => Mapper.Map<PhoneHistoryViewItem>(x))
                        .ToList();

                    PlusHashtags = string.Join(", ", result.PlusHashtagNames ?? Enumerable.Empty<string>());
                    MinusHashtags = string.Join(", ", result.MinusHashtagNames ?? Enumerable.Empty<string>());

                    Phones = result.Phones.ToReadOnlyObservableCollection();
                    SelectedPhones = result.Phones.ToObservableCollection();

                    if (result.Customers?.Length > 0)
                    {
                        Customers = result.Customers
                            .Select(x => new ComboBoxItem(
                                x.CustomerId,
                                string.IsNullOrEmpty(x.CustomerName) ? "Не зарегистрированный клиент" : x.CustomerName))
                            .ToReadOnlyObservableCollection();
                        SelectedCustomers = result.Customers.Select(x => x.CustomerId)
                            .ToObservableCollection();
                    }
                    else
                    {
                        Customers = new[] { new ComboBoxItem(0, "Не зарегистрированный клиент") }
                            .ToReadOnlyObservableCollection();
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                    MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Номера телефонов не должны быть пустыми");
            }

            IsCreateOrderButtonEnabled = true;
        }

        private async Task HandleRowDoubleClickAsync(PhoneHistoryViewItem item)
        {
            switch (item.TypeId)
            {
                case PhoneHistoryType.OrderId:
                    IsLongOperation = true;
                    Messenger.Send(new OrderEditViewMessage(item.DocumentNumber));
                    IsLongOperation = false;
                    break;
                case PhoneHistoryType.ServiceRequestId:
                    Messenger.Send(new ServiceRequestViewMessage(item.DocumentNumber));
                    break;
                case PhoneHistoryType.CallId:
                    if (item.State.Id == CallState.NewId)
                    {
                        CallDto call = await WebClient.ExecuteApiRequestAsync(new QueryCall(item.DocumentNumber));
                        CallViewItem callItem = Mapper.Map<CallViewItem>(call);
                        OutcomingCallViewMessage message = new OutcomingCallViewMessage(callItem);
                        Messenger.Send(message);
                    }
                    else
                    {
                        Messenger.Send(new CallViewMessage(item.DocumentNumber));
                    }

                    break;
                case PhoneHistoryType.ComplaintId:
                    Messenger.Send(new ComplaintViewMessage(item.DocumentNumber));
                    break;
                case PhoneHistoryType.TradeInId:
                    IsLongOperation = true;
                    Messenger.Send(new TradeInViewMessage(item.DocumentNumber));
                    IsLongOperation = false;
                    break;
            }
        }

        private void RefreshPhoneHistory()
        {
            PhoneHistory.Clear();
            PhoneHistory.AddRange(allPhoneHistory.Where(x =>
                (SelectedPhones != null && (SelectedPhones.Contains(x.Phone1) || SelectedPhones.Contains(x.Phone2)))
            || (SelectedCustomers != null && x.CustomerId.HasValue && SelectedCustomers.Contains(x.CustomerId.Value))));

            PhoneHistory = PhoneHistory.OrderByDescending(x => x.CreatedOn).ToObservableRangeCollection();
        }

        #region Order
        private async Task CreateOrderAsync()
        {
            IsLongOperation = true;

            if (SelectedPhones?.Any() == true)
            {
                string phone = SelectedPhones[0];

                IsCreateOrderButtonEnabled = false;

                CustomerFilteringItem item = new CustomerFilteringItem(phone);

                PagedResult<CustomerDto> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryCustomers(item)), "получении информации о клиенте", null, this, true, showNotification: false);

                if (result.Data?.Any() == true)
                {
                    CustomerDto customer = result.Data.First();

                    Messenger.Send(new OrderCreateViewMessage(customer));
                }
                else
                {
                    Messenger.Send(new OrderCreateViewMessage());
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Не выбран номер телефона");
            }

            IsLongOperation = false;
        }

        private void CreateComment(PhoneHistoryCommentParameter commentParameter)
        {
            string comment = GetDescription("Комментарий", commentParameter.Name, string.Empty);

            if (string.IsNullOrEmpty(comment))
            {
                return;
            }

            Messenger.Send(new CallDependencyMessage(MessageType.Added, GetCallDependencyTypeId(commentParameter.TypeId), commentParameter.Name, comment, CallStore.CallId, commentParameter.DocumentNumber));
        }

        private string GetDescription(string caption, string title, string content)
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                caption,
                title,
                regexPattern: @"^[\s\S]{2,255}$",
                errorMessage: "Допустимое количество символов от 2 до 255",
                content: content,
                isMultiline: true);

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            return fromUserViewModel.IsOk ? fromUserViewModel.Content : null;
        }

        private bool CanEditOrder(PhoneHistoryViewItem item)
        {
            return OrderButtonEnabled && SelectedPhoneHistory?.State.Id == OrderStatus.Received.Id;
        }

        private bool CanEditPayment(PhoneHistoryViewItem item)
        {
            return OrderButtonEnabled
                   && SelectedPhoneHistory?.State.Id == OrderStatus.Received.Id
                   && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.TechSupport, Role.Manager, Role.Seller, Role.Operator, Role.Product, Role.Accountant);
        }

        private bool CanMerge(PhoneHistoryViewItem item)
        {
            return OrderButtonEnabled
                   && SelectedPhoneHistory?.State.Id == OrderStatus.Received.Id
                   && WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.TechSupport, Role.Manager);
        }

        private bool CanConfirmOrder(PhoneHistoryViewItem item)
        {
            return OrderButtonEnabled && SelectedPhoneHistory?.State.Id == OrderStatus.Received.Id;
        }

        private bool CanEditCancelOrder(PhoneHistoryViewItem item)
        {
            return OrderButtonEnabled &&
                   (SelectedPhoneHistory?.State.Id == OrderStatus.Received.Id
                    || SelectedPhoneHistory?.State.Id == OrderStatus.Confirmed.Id
                    || SelectedPhoneHistory?.State.Id == OrderStatus.Packed.Id);
        }

        private async Task EditCancelOrderAsync(PhoneHistoryViewItem item)
        {
            IsLongOperation = true;

            await LockableOperationProcessor.DoOperationAsync(item.DocumentNumber, CancelOrderInternalAsync, false);

            IsLongOperation = false;
        }

        private async Task CancelOrderInternalAsync(OrderDto lockedOrder)
        {
            Result<OrderCancelInfoDto> result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryOrderCancelInfo(lockedOrder.Id)), "при получении данных", null, this, true, showNotification: false);

            OrderCancelInfoDto info = result?.Data;

            if (info != null)
            {
                DialogDocumentManagerService.ShowView<OrderCancelViewModel>(info, this);
            }
        }

        private async Task EditConfirmOrderAsync(PhoneHistoryViewItem item)
        {
            IsLongOperation = true;

            await LockableOperationProcessor.DoActionAsync(item.DocumentNumber, ConfirmOrderInternal, false);

            IsLongOperation = false;

            void ConfirmOrderInternal(OrderDto actualOrderObj)
            {
                DialogDocumentManagerService.ShowView<OrderConfirmViewModel>(actualOrderObj, this);
            }
        }

        private void EditOrder(PhoneHistoryViewItem item)
        {
            IsLongOperation = true;

            Messenger.Send(new OrderEditViewMessage(item.DocumentNumber, true));

            IsLongOperation = false;
        }

        private async Task EditPaymentAsync(PhoneHistoryViewItem item)
        {
            OrderDto orderFromServer = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryOrder(item.DocumentNumber)), "получении заказа", null, this, true, showNotification: false);

            if (orderFromServer != null)
            {
                if (orderFromServer.ExternalPayments?.Any(x => x.PaymentId == Payment.LiqPayId
                                                               && (x.PaymentStateId == PaymentState.Confirmed.Id || x.PaymentStateId == PaymentState.Created.Id)) == true)
                {
                    MessageFacadeService.ShowNotificationWarning("Запрещено менять способ оплаты LiqPay до принятия решения по списанию");
                    return;
                }


                if (orderFromServer.ExternalPayments?.Any(x => x.PaymentId == Payment.MonoPayId
                                                               && (x.PaymentStateId == PaymentState.Confirmed.Id || x.PaymentStateId == PaymentState.Created.Id)) == true)
                {
                    MessageFacadeService.ShowNotificationWarning("Запрещено менять способ оплаты MonoPay до принятия решения по списанию");
                    return;
                }


                if (orderFromServer.ExternalPayments?.Any(x => x.PaymentId == Payment.NovaPayId
                                                               && (x.PaymentStateId == PaymentState.Confirmed.Id
                                                                   || x.PaymentStateId == PaymentState.Created.Id
                                                                   || x.PaymentStateId == PaymentState.Processing.Id)) == true)
                {
                    MessageFacadeService.ShowNotificationWarning("Запрещено менять способ оплаты NovaPay до принятия решения по списанию");
                    return;
                }

                if (orderFromServer.ExternalPayments?.Any(x => x.PaymentId == Payment.PortmoneId
                                                               && (x.PaymentStateId == PaymentState.Confirmed.Id
                                                                   || x.PaymentStateId == PaymentState.Created.Id)) == true)
                {
                    MessageFacadeService.ShowNotificationWarning("Запрещено менять способ оплаты Portmone (Ощадбанк) до принятия решения по списанию");
                    return;
                }

                await LockableOperationProcessor.DoOperationAsync(item.DocumentNumber, EditPayment, false);

                Task EditPayment(OrderDto order)
                {
                    DialogDocumentManagerService.ShowView<OrderEditPaymentViewModel>(new OrderEditPaymentParameter(order.Id, order.SubdivisionId, order.PaymentId, order.GetTotalAmount().Uah, changePayment: true), this);
                    return Task.CompletedTask;
                }
            }
        }

        private async Task EditMergeOrderAsync(PhoneHistoryViewItem item)
        {
            OrderDto orderFromServer = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryOrder(item.DocumentNumber)), "получении заказа", null, this, true, showNotification: false);

            if (orderFromServer == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выбранный заказ не найден");
                return;
            }

            if (orderFromServer.EmployeeLockId.HasValue && orderFromServer.EmployeeLockId != WebClient.AuthenticatedEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning($"Заказ уже заблокирован пользователем {orderFromServer.EmployeeLock.ShortName}");
                return;
            }

            if (Dictionaries.GetItemById<Subdivision>(orderFromServer.SubdivisionId).IsRetail)
            {
                MessageFacadeService.ShowNotificationWarning("Функция объединения доступна только для оптовых заказов");
                return;
            }

            if (orderFromServer.CityId == null || orderFromServer.WarehouseId == null)
            {
                MessageFacadeService.ShowNotificationWarning("В заказе не заполнен город или склад");
                return;
            }

            List<int> wholesaleSubdivisionsIds = Dictionaries.GetItems<Subdivision>()
                .Where(x => !x.IsRetail)
                .Select(x => x.Id)
                .ToList();

            OrderFilteringItem filteringItem = new OrderFilteringItem(null, wholesaleSubdivisionsIds)
            {
                Contractors = new List<int> { orderFromServer.ClientId },
                Cities = new List<int> { orderFromServer.CityId.Value },
                Carries = new List<int> { orderFromServer.CarryId },
                Warehouses = new List<int> { orderFromServer.WarehouseId.Value },
                Payments = new List<int> { orderFromServer.PaymentId },
                OrderStatuses = new List<int> { OrderStatus.Received.Id }
            };

            PagedResult<OrderDto> pagedResult = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryOrders(filteringItem)), "получении заказов для объединения", null, this, true, showNotification: false);

            List<OrderDto> orderDtos = pagedResult.Data.Except(pagedResult.Data.Where(x => x.Id == orderFromServer.Id)).ToList();

            if (orderDtos.Any())
            {
                MergeOrdersParameter parameter = new MergeOrdersParameter(orderFromServer, orderDtos);

                MergeOrdersViewModel viewModel = DialogDocumentManagerService.ShowView<MergeOrdersViewModel>(parameter, this);

                if (viewModel.IsOk)
                {
                    PhoneHistoryResultDto result = await ErrorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new QueryPhoneHistory(phones)), "получении истории клиента", null, this, true, showNotification: false);

                    allPhoneHistory = result.History
                        .OrderByDescending(x => x.CreatedOn)
                        .Select(x => Mapper.Map<PhoneHistoryViewItem>(x))
                        .ToList();

                    RefreshPhoneHistory();
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Нечего объединять");
            }
        }
        #endregion

        #region ServiceRequest
        private void CreateServiceRequest(PhoneHistoryViewItem item)
        {
            if (item != null && item.TypeId == PhoneHistoryType.OrderId)
            {
                Messenger.Send(new ServiceRequestCreateFromPhoneHistoryMessage(item.DocumentNumber));
            }
            else
            {
                Messenger.Send(new ServiceRequestCreateViewMessage());
            }
        }

        #endregion

        #region Complain

        private async Task CreateComplaintAsync(PhoneHistoryCommentParameter item)
        {
            ComplaintCreateParameter parameter = ComplaintCreateParameter.Empty;

            if (item.DocumentNumber.HasValue)
            {
                switch (item.TypeId)
                {
                    case PhoneHistoryType.OrderId:

                        parameter = await ComplaintCreateParameterByOrderAsync(item.DocumentNumber.Value);

                        break;
                    case PhoneHistoryType.ServiceRequestId:

                        parameter = await ComplaintCreateParameterByServiceRequestAsync(item.DocumentNumber.Value);

                        break;
                    case PhoneHistoryType.TradeInId:

                        parameter = await ComplaintCreateParameterByTradeInAsync(item.DocumentNumber.Value);

                        break;
                }
            }

            NonModalDialogDocumentManagerService.ShowView<ComplaintCreateViewModel>(parameter, this);
        }

        private async Task<ComplaintCreateParameter> ComplaintCreateParameterByOrderAsync(int orderId)
        {
            OrderDto orderDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId)),
                "получении сервесной заявки",
                null,
                this,
                true,
                showNotification: false);

            if (orderDto == null)
            {
                return ComplaintCreateParameter.Empty;
            }

            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                orderDto.Id,
                null,
                null,
                orderDto.ClientId,
                orderDto.Products.Select(x => new ComboBoxItem(x.Product.Id, x.Product.Name)).ToList(),
                orderDto.Fio,
                orderDto.Phone,
                orderDto.Phone2,
                orderDto.Email);

            return parameter;
        }

        private async Task<ComplaintCreateParameter> ComplaintCreateParameterByServiceRequestAsync(int serviceRequestId)
        {
            ServiceRequestDto serviceRequest = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId)),
                "получении сервесной заявки",
                null,
                this,
                true,
                showNotification: false);

            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                serviceRequest.OrderId,
                serviceRequest.Id,
                null,
                serviceRequest.ContractorId,
                new[] { new ComboBoxItem(serviceRequest.ProductId, serviceRequest.ProductName) },
                serviceRequest.Fio,
                serviceRequest.Phone,
                serviceRequest.Phone2,
                serviceRequest.Email);

            return parameter;
        }

        private async Task<ComplaintCreateParameter> ComplaintCreateParameterByTradeInAsync(int tradeInId)
        {
            TradeInDto tradeIn = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeIn(tradeInId)),
                "получении Trade-In заявки",
                null,
                this,
                true,
                showNotification: false);

            ComplaintCreateParameter parameter = new ComplaintCreateParameter(
                null,
                null,
                tradeIn.Id,
                null,
                new[] { new ComboBoxItem(tradeIn.ProductId ?? 0, tradeIn.ProductName) },
                $"{tradeIn.LastName} {tradeIn.FirstName} {tradeIn.MiddleName}",
                tradeIn.Phone,
                null,
                tradeIn.Email);

            return parameter;
        }

        #endregion

        #region Other
        private void CreateCommentConsultation(PhoneHistoryCommentParameter commentParameter)
        {
            string comment = GetDescription("Комментарий", commentParameter.Name, string.Empty);

            if (string.IsNullOrEmpty(comment))
            {
                return;
            }

            Messenger.Send(new CallDependencyMessage(MessageType.Added, GetCallDependencyTypeId(commentParameter.TypeId), commentParameter.Name, comment, CallStore.CallId, commentParameter.DocumentNumber));
        }
        #endregion

        private void CallDependencyWarning(CallDependencyWarningMessage message)
        {
            string comment = GetDescription("Комментарий", message.Name, message.Comment);

            if (string.IsNullOrEmpty(comment))
            {
                return;
            }

            Messenger.Send(new CallDependencyMessage(MessageType.Added, message.DependencyTypeId, message.Name, comment, message.CallId, message.DocumentId));
        }

        private int? GetCallDependencyTypeId(int? phoneHistoryTypeId)
        {
            switch (phoneHistoryTypeId)
            {
                case PhoneHistoryType.OrderId:
                    return CallDependencyType.OrderId;
                case PhoneHistoryType.ServiceRequestId:
                    return CallDependencyType.ServiceRequestId;
                case PhoneHistoryType.ComplaintId:
                    return CallDependencyType.ComplaintId;
                default:
                    return null;
            }
        }
    }
}