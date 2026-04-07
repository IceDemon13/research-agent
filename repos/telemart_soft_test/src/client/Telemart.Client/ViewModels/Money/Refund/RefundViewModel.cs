using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.PosTerminal;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.Requests.Features.Refund;
using Telemart.Client.Data.Requests.Features.Refund.Actions;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Refund;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;
using ProductType = Telemart.Common.Dictionaries.ProductType;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public sealed class RefundViewModel : TelemartEditorViewModelBase<RefundDto, RefundViewMessage, RefundViewItem>
    {
        private readonly bool allowRefundTerminalMoney;

        private IReadOnlyDictionary<int, string> employees;
        private IReadOnlyDictionary<int, string> contractors;
        private List<OrderPaymentDto> orderPayments;
        private OrderDto order;
        private WarehouseDto orderWarehouse;

        private List<CashboxDto> cashboxes;
        private ReturnRefundResult _terminalRefundResult;

        public RefundViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IPosTerminalFactory posTerminalFactory,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator,
            IErrorHandler errorHandler,
            IRroPrintHelper rroPrintHelper)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            PosTerminalFactory = posTerminalFactory;
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            ErrorHandler = errorHandler;
            RroPrintHelper = rroPrintHelper;

            OpenOrderCommand = new DelegateCommand<int>(OpenOrder);
            OpenServiceRequestCommand = new DelegateCommand<int>(OpenServiceRequest);
            ConfirmCommand = new AsyncCommand(ConfirmAsync, CanConfirm);
            RefundCommand = new AsyncCommand(RefundAsync, CanRefund);
            SendSmsCommand = new AsyncCommand<string>(SendSmsAsync, x => !string.IsNullOrWhiteSpace(x));
            HandlePaymentChangedCommand = new DelegateCommand(HandlePaymentChanged);
            SetRequisitesCommand = new DelegateCommand(SetRequisites, () => Model != null && (IsNewOrIsLockedByCurrentEmployee || Model.StateId == RefundState.Done.Id) && Model.Payment?.RefundRequisitesControl == true);
            PrintFiscalReturnChequeCommand = new AsyncCommand(PrintFiscalReturnChequeAndConfirmAsync, () => Model != null && !string.IsNullOrEmpty(Model.FiscalId) && Model.State == RefundState.Done);
            CancelRefundCommand = new AsyncCommand(CancelAsync, CanCancel);
            RevertRefundCommand = new AsyncCommand(RevertAsync, () => Model != null && Model.StateId == RefundState.Done.Id && Model.Payment?.RefundRevertAllowed == true && WebClient.IsOperationAllowed(BusinessOperation.RefundRevert));
            PrintRefundDocumentCommand = new AsyncCommand(PrintRefundDocumentAsync, () => IsPrintVisible && Model.Payment?.Id != Payment.TerminalId && Model.FiscalId != null);
            PrintStatementReturnRefundCommand = new AsyncCommand(PrintStatementReturnRefundAsync, () => IsPrintVisible);

            allowRefundTerminalMoney = WebClient.IsOperationAllowed(BusinessOperation.RefundTerminalMoney);

            Messenger.Register<RefundUpdateRequisitesMessage>(this, RefundUpdateRequisites);
        }

        public RefundViewModel()
        {
        }

        #region Commands

        public IDelegateCommand OpenOrderCommand { get; }

        public IDelegateCommand OpenServiceRequestCommand { get; }

        public IDelegateCommand SetRequisitesCommand { get; }

        public IAsyncCommand PrintRefundDocumentCommand { get; }

        public IAsyncCommand PrintStatementReturnRefundCommand { get; }

        public IAsyncCommand ConfirmCommand { get; }

        public IAsyncCommand RefundCommand { get; }

        public IAsyncCommand SendSmsCommand { get; }

        public IDelegateCommand HandlePaymentChangedCommand { get; }

        public IAsyncCommand PrintFiscalReturnChequeCommand { get; }

        public IAsyncCommand CancelRefundCommand { get; }

        public IAsyncCommand RevertRefundCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<Payment> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> OrderPaymentsTransactions
        {
            get { return GetProperty(() => OrderPaymentsTransactions); }
            set { SetProperty(() => OrderPaymentsTransactions, value); }
        }

        public bool TransactionVisible
        {
            get { return GetProperty(() => TransactionVisible); }
            set { SetProperty(() => TransactionVisible, value); }
        }

        public bool IsPrintVisible => Model != null;

        #endregion

        protected override string CreatedActionMessage => "создан";

        protected override string EntityName => "Возврат";

        protected override string UpdatedActionMessage => "сохранен";

        private IPosTerminalFactory PosTerminalFactory { get; }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        private IRroPrintHelper RroPrintHelper { get; }

        protected override Task<Result<RefundDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(RefundDto dto, MessageType messageType)
        {
            return new RefundMessage(dto, messageType);
        }

        protected override Task<RefundDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryRefund(id));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(Model.OrderClientId);
            yield return nameof(Model.OrderId);
            yield return nameof(Model.OrderSubdivisionId);
            yield return nameof(Model.ServiceRequestId);
            yield return nameof(Model.Amount);
            yield return nameof(Model.EmployeeLockId);
            yield return nameof(Model.EmployeeLockName);
            yield return nameof(Model.Payment.Name);
            yield return nameof(Model.Payment.Active);
        }

        protected override async Task HandleLoadedAsync()
        {
            RefundViewMessage parameter = (RefundViewMessage)Parameter;

            if (parameter.IsNew)
            {
                throw new NotSupportedException("Refund creation is not supported");
            }

            await Task.WhenAll(RefreshCashboxesAsync(), RefreshContractorsAsync(), RefreshEmployeesAsync());

            await base.HandleLoadedAsync();

            await Task.WhenAll(FetchOrderAsync(), FetchOrderPaymentsAsync());

            HandlePaymentChanged();

            int[] orderPaymentTypeIds = orderPayments
                .Where(x => x.Sign > 0 && x.PaymentId.HasValue)
                .Select(x => x.PaymentId.Value)
                .Distinct()
                .ToArray();

            List<Payment> paymentTypes = Dictionaries
                .GetRefundPayments(orderPaymentTypeIds, null)
                .Where(x => x.Active)
                .OrderBy(x => x.Id)
                .ToList();

            if (!paymentTypes.Any())
            {
                paymentTypes.AddRange(Dictionaries.GetItems<Payment>().Where(x => x.Id is Payment.CashId or Payment.BankId));
            }

            if (Model.Payment?.Id != null && paymentTypes.All(x => x.Id != Model.Payment.Id))
            {
                paymentTypes.Add(Dictionaries.GetItemById<Payment>(Model.Payment.Id));
            }

            Payments = paymentTypes.ToReadOnlyObservableCollection();

            Payments = Model.Payment?.Id == Payment.BonusesId
                ? paymentTypes.Where(x => x.Id == Payment.BonusesId).ToReadOnlyObservableCollection()
                : paymentTypes.ToReadOnlyObservableCollection();

            OrderPaymentsTransactions = await GetOrderPaymentsTransactionsAsync();

            RefreshSummaryItems();

            RaisePropertyChanged(nameof(IsPrintVisible));

            HandlePaymentChangedCommand.Execute(null);

            async Task RefreshCashboxesAsync()
            {
                cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);
            }

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
                employees = employeesList.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task RefreshContractorsAsync()
            {
                List<ContractorDto> contractorsList = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                contractors = contractorsList.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task FetchOrderAsync()
            {
                order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(Model.OrderId));
            }

            async Task FetchOrderPaymentsAsync()
            {
                orderPayments = await WebClient.ExecuteApiRequestAsync(new QueryOrderPayments(Model.OrderId));
            }
        }

        protected override Task<LockResponse<RefundDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockRefund(id));
        }

        protected override Task<LockResponse<RefundDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockRefund(id));
        }

        protected override void AfterSetData()
        {
            if (order != null)
            {
                Model.OrderStateId = order.StateId;
                ModelOriginal.OrderStateId = order.StateId;
                RefreshSummaryItems();
            }
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание возврата";
        }

        protected override void SetEditTitle()
        {
            Title = $"Возврат №{Model.Id.ToString()}";
        }

        protected override Task<Result<RefundDto>> UpdateEntityAsync()
        {
            RefundSaveDto saveDto = Mapper.Map<RefundSaveDto>(Model);
            saveDto.RefundRequisites = MapRequisites(Model);
            UpdateRefund gatewayRequest = new UpdateRefund(Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override bool CanEdit()
        {
            return !Model.IsCompleted;
        }

        protected override void OnInitializeInDesignMode()
        {
            SummaryItems = GetItems();

            static IEnumerable<SummaryViewItem> GetItems()
            {
                yield return new SummaryViewItem("Контрагент", "!Telemart");
                yield return new SummaryViewItem("Валюта", "USD");
                yield return new SummaryViewItem("Статус", "State");
                yield return new SummaryViewItem("Создал", "23.05.17 23:05");
                yield return new SummaryViewItem("Подтвердил", "23.12.17 14:20");
                yield return new SummaryViewItem("Выполнил", "04.01.16 11:30");
            }
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RefundViewItem.Payment):
                {
                    CashboxDto[] cashboxesByPayment = cashboxes.ForReturn(Model.CurrencyId, Model.Payment?.Id).ToArray();

                    Model.CashboxNotValid = cashboxesByPayment.Any(x => x.Id == Model.CashboxId) != true;
                }

                break;
                case nameof(RefundViewItem.CashboxId):
                {
                    CashboxDto[] cashboxesByPayment = cashboxes.ForReturn(Model.CurrencyId, Model.Payment?.Id).ToArray();

                    Model.CashboxNotValid = cashboxesByPayment.Any(x => x.Id == Model.CashboxId) != true;
                }

                break;
            }

            base.OnModelPropertyChangedInternal(sender, e);
        }

        private static RefundRequisitesDto MapRequisites(RefundViewItem source)
        {
            return new RefundRequisitesDto(
                source.FirstName,
                source.LastName,
                source.MiddleName,
                source.Iban,
                source.CardNumber,
                source.Inn);
        }

        private void HandlePaymentChanged()
        {
            Cashboxes = cashboxes
                .ForReturn(Model.CurrencyId, Model.Payment?.Id, Model.CashboxId)
                .ForLegalEntity(Model.LegalEntity, selectedCashboxId: Model.CashboxId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Model.OrderPaymentId = Model.Payment?.Id == Payment.TerminalId ? Model.OrderPaymentId : null;
            TransactionVisible = Model.Payment?.Id == Payment.TerminalId;
        }

        private void SetRequisites()
        {
            RefundRequisitesParameter parameter = new RefundRequisitesParameter(
                new RequisitesViewItem()
                {
                    Iban = Model.Iban,
                    Inn = Model.Inn,
                    CardNumber = Model.CardNumber,
                    FirstName = Model.FirstName ?? order.FirstName,
                    LastName = Model.LastName ?? order.LastName,
                    MiddleName = Model.MiddleName ?? order.MiddleName,
                    Required = Model.EmployeeLockId.HasValue,
                    Enabled = Model.EmployeeLockId.HasValue,
                    Visible = true
                },
                Model.Id);

            NonModalDialogDocumentManagerService.ShowView<RefundRequisitesViewModel>(parameter, this);
        }

        private async Task PrintFiscalReturnChequeAndConfirmAsync()
        {
            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (string.IsNullOrEmpty(Model.FiscalId) && clientResult.IsSuccess)
            {
                await CreateFiscalReturnChequeAsync(clientResult.Data);
            }
            else if (clientResult.IsSuccess)
            {
                await PrintFiscalReturnChequeAsync(Model.FiscalId, clientResult.Data);
            }
        }

        private async Task PrintFiscalReturnChequeAsync(string fiscalId, IFiscalRegistrarClient client)
        {
            if (client != null)
            {
                Result resultSentRroCheck = await RroPrintHelper.SentCheckAsync(fiscalId, order.Phone, order.Email, client.Settings.CashboxId, this);

                if (resultSentRroCheck.IsSuccess)
                {
                    await ConfirmRefundOnFiscalRegistrarAsync(fiscalId, true);
                }
            }
        }

        private async Task CreateFiscalReturnChequeAsync(IFiscalRegistrarClient clientFiscal)
        {
            if (Model.Payment == null)
            {
                MessageFacadeService.ShowNotificationWarning("В возврате не заполнен способ оплаты");
                return;
            }

            if (!Model.CashboxId.HasValue)
            {
                MessageFacadeService.ShowNotificationWarning("Сперва заполните кассу");
                return;
            }

            int cashboxId = clientFiscal.Settings.CashboxId;

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            if (cashbox.Session?.Closed != false)
            {
                MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                return;
            }

            if (cashbox.LegalEntityId.HasValue != true)
            {
                MessageFacadeService.ShowNotificationWarning("Юр.лицо в кассе не заполнено");
                return;
            }

            string prefixComment = $"Касир: {WebClient.AuthenticatedEmployee.ShortName}";

            int orderId;
            int entityId, entityTypeId;

            ServiceRequestDto serviceRequest = null;

            if (order.BasedOnServiceRequestId == null && Model.ServiceRequestId == null)
            {
                orderId = Model.OrderId;
                entityId = Model.Id;
                entityTypeId = Entity.RefundId;
            }
            else
            {
                serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(Model.ServiceRequestId ?? order.BasedOnServiceRequestId ?? 0));
                orderId = order.BasedOnServiceRequestId.HasValue ? order.Id : serviceRequest.OrderId;
                entityId = serviceRequest.Id;
                entityTypeId = Entity.ServiceRequestId;
            }

            IReadOnlyCollection<SellProductRequest> products = new[]
            {
                new SellProductRequest(
                    FiscalRegistrarConstants.PrepaymentProductId,
                    "Передплата",
                    null,
                    Model.Amount,
                    1,
                    ProductType.Product)
            };

            string postfixComment = $"Повернення коштів №{Model.Id}";

            ReturnRequest returnRequest = new ReturnRequest(
                Guid.NewGuid().ToString(),
                cashbox.Id,
                prefixComment,
                postfixComment,
                orderId,
                products,
                new[]
                {
                    new SellPaymentRequest(FiscalPaymentType.GetByPaymentId(Model.Payment.Id), Model.Amount)
                    {
                        CheckNumber = _terminalRefundResult?.CheckNumber,
                        Rrn = _terminalRefundResult?.Rrn,
                        TerminalId = _terminalRefundResult?.TerminalId,
                        MerchantId = _terminalRefundResult?.MerchantId,
                        AuthCode = _terminalRefundResult?.AuthCode,
                        Pan = _terminalRefundResult?.Pan,
                        IssuerName = _terminalRefundResult?.IssuerName
                    }
                },
                Array.Empty<string>(),
                null);

            await ConfirmRefundOnFiscalRegistrarAsync(returnRequest.OperationId, false);

            Result<SellResponse> returnResult = await ErrorHandler.HandleErrorsAsync(
                ct => clientFiscal.ReturnAsync(returnRequest, ct),
                "проведении возврата через РРО",
                "Возврат в РРО проведен",
                this,
                true);

            Result<QueryReceiptStatusResponse> statusResult = await clientFiscal.GetReceiptStatusAsync(cashbox.Id, returnRequest.OperationId, default);

            if (!statusResult.IsSuccess || statusResult.Data.Status == "ERROR")
            {
                await ConfirmRefundOnFiscalRegistrarAsync(null, false);

                MessageFacadeService.ShowNotificationInfo("Ошибка при проверке статуса чека");

                return;
            }

            await PrintFiscalReturnChequeAsync(returnRequest.OperationId, clientFiscal);

            await PrintRefundDocumentInternalAsync(serviceRequest, cashbox.LegalEntity);



            await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(returnRequest.ToDocument(cashbox.Id, returnRequest.OperationId, entityId, entityTypeId)));
        }

        private async Task ConfirmRefundOnFiscalRegistrarAsync(string fiscalId, bool printed)
        {
            Result<RefundDto> result = await WebClient.ExecuteApiRequestAsync(new ConfirmRefundOnFiscalRegistrar(Model.Id, fiscalId, printed));

            SetData(result.Data);
        }

        private async Task PrintRefundDocumentAsync()
        {
            int?[] rroCashboxIds = cashboxes
                .Where(x => x.TypeId == CashboxType.FiscalRegistrar.Id)
                .Select(x => x.Id as int?)
                .ToArray();

            if (!Model.CashboxId.HasValue)
            {
                MessageFacadeService.ShowNotificationWarning("Сперва заполните кассу");
                return;
            }

            if (!rroCashboxIds.Contains(Model.CashboxId))
            {
                MessageFacadeService.ShowNotificationError("Касса должна быть РРО");
                return;
            }

            CashboxDto cashboxDto = cashboxes?.FirstOrDefault(x => x.Id == Model.CashboxId);

            if (cashboxDto?.LegalEntityId.HasValue != true)
            {
                MessageFacadeService.ShowNotificationError("В кассе не заполнено юр.лицо");
                return;
            }

            ServiceRequestDto serviceRequest = null;

            if (order.BasedOnServiceRequestId != null || Model.ServiceRequestId != null)
            {
                serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(Model.ServiceRequestId ?? order.BasedOnServiceRequestId ?? 0));
            }

            await PrintRefundDocumentInternalAsync(serviceRequest, cashboxDto.LegalEntity);
        }

        private async Task PrintStatementReturnRefundAsync()
        {
            string productName = string.Empty;

            if (Model.ServiceRequestId.HasValue)
            {
                ServiceRequestDto serviceRequestDto = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(Model.ServiceRequestId.Value));

                productName = serviceRequestDto.ProductName;
            }

            StatementReturnRefundData data = new StatementReturnRefundData(order.Fio, order.Phone, order.Email, order.CreatedOn, productName, Model.Iban, Model.Inn, Model.CardNumber);

            StatementReturnRefundReport report = new StatementReturnRefundReport() { DataSource = new[] { data } };

            PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

            PrintReportRequest printRequest = printSettings?.Main != null
                ? new PrintReportRequest(report, $"StatementReturnRefund_{Model.Id}.pdf", true, printSettings.Main.Name, printSettings.Main.PaperSource)
                : new PrintReportRequest(report, $"StatementReturnRefund_{Model.Id}.pdf", true);

            await Mediator.Send(printRequest);
        }

        private async Task PrintRefundDocumentInternalAsync(ServiceRequestDto serviceRequest, LegalEntityDto legalEntity)
        {
            try
            {
                RefundReportData reportData = serviceRequest != null
                    ? new RefundReportData(WebClient.AuthenticatedEmployee.ShortName, Model.Amount, legalEntity.ReportName, legalEntity.Edrpou, serviceRequest.ProductFullNameUkr)
                    : new RefundReportData(WebClient.AuthenticatedEmployee.ShortName, Model.Amount, legalEntity.ReportName, legalEntity.Edrpou);

                RefundReport report = new RefundReport { DataSource = new[] { reportData } };

                PrintingSettingsInfo printSettings = await PrintingSettingsStore.LoadAsync();

                PrintReportRequest printRequest = printSettings?.Main != null
                    ? new PrintReportRequest(report, $"RefundReport_{Model.Id}.pdf", true, printSettings.Main.Name, printSettings.Main.PaperSource)
                    : new PrintReportRequest(report, $"RefundReport_{Model.Id}.pdf", true);

                await Mediator.Send(printRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                    new ValidationResultViewModelParameter("Ошибки", exception.GetErrorItems()),
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
                Logger.LogError(exception, "Failed to print");
                MessageFacadeService.ShowNotificationError("Ошибка печати");
            }
        }

        private bool CanConfirm()
        {
            return Model != null && Model.StateId == RefundState.New.Id && !IsNew && Model.EmployeeLockId == null;
        }

        private Task ConfirmAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно хотите подтвердить возврат ДС?"))
            {
                return Task.CompletedTask;
            }

            if (Model.Payment?.RefundRequisitesControl == true
                && (string.IsNullOrWhiteSpace(Model.FirstName)
                    || string.IsNullOrWhiteSpace(Model.LastName)
                    || string.IsNullOrWhiteSpace(Model.MiddleName)
                    || string.IsNullOrWhiteSpace(Model.Iban)
                    || string.IsNullOrWhiteSpace(Model.Inn)
                    || string.IsNullOrWhiteSpace(Model.CardNumber)))
            {
                CashboxDto cashbox = cashboxes.FirstOrDefault(x => x.Id == Model.CashboxId);

                if (cashbox is null || cashbox.TypeId != CashboxType.Virtual.Id)
                {
                    MessageFacadeService.ShowNotificationError("Реквизиты не заполнены");
                    return Task.CompletedTask;
                }
            }

            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(
                        new ConfirmRefund(lockedEntity.Id)),
                    "подтверждении возврата ДС",
                    "Возврат ДС подтвержден",
                    this,
                    true);
            });
        }

        private bool CanRefund()
        {
            return Model != null && Model.StateId == RefundState.Confirmed.Id && !IsNew && Model.EmployeeLockId == null;
        }

        private async Task RefundAsync()
        {
            string infoRefandQuestion = "Вы действительно хотите вернуть ДС?";

            if (Model.Payment?.Id == Payment.TerminalId)
            {
                infoRefandQuestion = "Для осуществления возврата необходимо приложить карту с которой были списаны средства.\n" + infoRefandQuestion;
            }

            if (!MessageFacadeService.Confirm(infoRefandQuestion))
            {
                return;
            }

            await ExecuteLockableOperationAsync(async _ =>
            {
                bool isCanRefund = await CanRefundAmountAsync();

                if (!isCanRefund)
                {
                    return;
                }

                if (Model.CashboxId == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Касса не заполнена");
                    return;
                }

                IFiscalRegistrarClient fiscalRegistrarClient = null;
                IPosTerminalClient posTerminalClient = null;

                if (Model.Payment?.Id == Payment.TerminalId)
                {
                    Result<IPosTerminalClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                        _ => PosTerminalFactory.CreateAsync(Model.LegalEntity?.Id ?? 0),
                        "Определении настроек терминала",
                        null,
                        this,
                        true);

                    if (!clientResult.IsSuccess)
                    {
                        return;
                    }

                    posTerminalClient = clientResult.Data;
                }

                CashboxDto refundCashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(Model.CashboxId.Value));

                Payment payment = Dictionaries.GetItemById<Payment>(order.PaymentId);

                bool useFiscal = Model.LegalEntity.FiscalCashboxId != refundCashbox.Id
                                 && Model.LegalEntity.White
                                 && refundCashbox.TypeId == CashboxType.FiscalRegistrar.Id
                                 && payment.Fiscal;

                if (useFiscal)
                {
                    Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                        ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                        "Определении настроек РРО",
                        null,
                        this,
                        true);

                    if (!clientResult.IsSuccess)
                    {
                        return;
                    }

                    fiscalRegistrarClient = clientResult.Data;

                    if (Model.Payment?.Id != Payment.TerminalId && Model.CashboxId != fiscalRegistrarClient.Settings.CashboxId)
                    {
                        MessageFacadeService.ShowNotificationWarning("Касса возврата не равна кассе в настройках");
                        return;
                    }

                    CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(fiscalRegistrarClient!.Settings.CashboxId));

                    if (cashbox.Session?.Closed != false)
                    {
                        MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                        return;
                    }
                }

                if (Model.Payment?.Id == Payment.TerminalId)
                {
                    bool isOk = await RefundAmountWithTerminalAsync(fiscalRegistrarClient, posTerminalClient);

                    if (!isOk)
                    {
                        return;
                    }
                }

                Result<RefundDto> result = await WebClient.ExecuteApiRequestAsync(new RefundRequest(Model.Id));
                MessageFacadeService.ShowNotificationInfo($"Возврат ДС №{result.Data.Id} проведен");

                if (useFiscal)
                {
                    await PrintFiscalReturnChequeAndConfirmAsync();
                }
                else
                {
                    if (Model.LegalEntity.FiscalCashboxId != refundCashbox.Id)
                    {
                        await ConfirmRefundOnFiscalRegistrarAsync(null, true);
                    }
                }
            });
        }

        private void OpenOrder(int orderId)
        {
            Messenger.Send(new OrderEditViewMessage(orderId));
        }

        private void OpenServiceRequest(int serviceRequestId)
        {
            Messenger.Send(new ServiceRequestViewMessage(serviceRequestId));
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                const string Format = DateFormattingRules.FullDateTimeFormat;

                yield return new SummaryViewItem("Контрагент", contractors.GetValueOrDefault(Model.OrderClientId));
                yield return new SummaryViewItem("Валюта", Currency.GetById(Model.CurrencyId).Title);
                yield return new SummaryViewItem("Статус", Model.State.Name);

                yield return new SummaryViewItem("Создал", $"{employees.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(Format)})");

                if (Model.ApprovedBy.HasValue && Model.ApprovedOn.HasValue)
                {
                    yield return new SummaryViewItem("Подтвердил", $"{employees.GetValueOrDefault(Model.ApprovedBy.Value)} ({Model.ApprovedOn.Value.ToString(Format)})");
                }

                if (Model.PayedBy.HasValue && Model.PayedOn.HasValue)
                {
                    yield return new SummaryViewItem("Выполнил", $"{employees.GetValueOrDefault(Model.PayedBy.Value)} ({Model.PayedOn.Value.ToString(Format)})");
                }

                if (!string.IsNullOrEmpty(Model.CardNumber))
                {
                    yield return new SummaryViewItem("Карта", $"{Model.CardNumber}");
                }

                if (!string.IsNullOrEmpty(Model.Iban))
                {
                    yield return new SummaryViewItem("IBAN", $"{Model.Iban}");
                }

                if (!string.IsNullOrEmpty(Model.Inn))
                {
                    yield return new SummaryViewItem("ИНН", $"{Model.Inn}");
                }

                if (!string.IsNullOrEmpty(Model.FirstName) || !string.IsNullOrEmpty(Model.MiddleName) || !string.IsNullOrEmpty(Model.LastName))
                {
                    yield return new SummaryViewItem("ФИО", $"{Model.LastName} {Model.FirstName} {Model.MiddleName}");
                }

                if (!string.IsNullOrWhiteSpace(Model.FiscalId))
                {
                    yield return new SummaryViewItem("Фиск. чек", SignalsConstants.TrueStr);
                }
            }
        }

        private async Task SendSmsAsync(string phone)
        {
            if (Model.State != RefundState.Confirmed)
            {
                MessageFacadeService.ShowNotificationWarning("Отправлять SMS можно только в статусе \"Подтвержден\"");
                return;
            }

            if (!MessageFacadeService.Confirm("Отправить повторно клиенту SMS с приглашением?"))
            {
                return;
            }

            if (order.WarehouseId == null)
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнен склад в заказе");
                return;
            }

            orderWarehouse ??= await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(order.WarehouseId.Value));

            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new SendSmsTemplate(
                    SmsTemplate.RefundNotifyId,
                    phone,
                    Model.OrderId,
                    Model.ServiceRequestId,
                    null,
                    null,
                    JObject.FromObject(new
                    {
                        AmountStr = CurrencyFormatingRules.ToStr(Model.Amount, Model.CurrencyId),
                        WarehouseAddress = orderWarehouse.AddressUa,
                        WarehouseInfo = orderWarehouse.Info
                    }),
                    SendMessageType.HybridId)),
                "отправке СМС",
                "Смс создано (добавлено в очередь\nна отправку согласно рабочему графику)",
                this,
                true);
        }

        private Task RevertAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно хотите переоткрыть возврат ДС?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async _ =>
            {
                Result<RefundDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new RevertRefund(Model.Id)),
                    "переоткрытии",
                    "Возврат ДС переоткрыт",
                    this,
                    true,
                    showNotification: true);

                if (result?.IsSuccess == true)
                {
                    MessageFacadeService.ShowMessageBoxWarning("Не забудьте переоткрыть возврат ДС в 1С");
                }
            });
        }

        private Task CancelAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно хотите отменить возврат ДС?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async _ =>
            {
                Result<RefundDto> result = await WebClient.ExecuteApiRequestAsync(new CancelRefund(Model.Id));
                MessageFacadeService.ShowNotificationInfo($"Возврат ДС №{result.Data.Id} отменен");
            });
        }

        private bool CanCancel()
        {
            return Model != null && WebClient.IsOperationAllowed(BusinessOperation.RefundCancel) && Model.EmployeeLockId == null && (Model.StateId == RefundState.New.Id || Model.StateId == RefundState.Confirmed.Id);
        }

        private string GetNameOrderPayment(OrderPaymentDto dto)
        {
            StringBuilder builder = new StringBuilder(50);

            CashboxDto cashboxDto = cashboxes.FirstOrDefault(x => x.Id == dto.CashboxId);

            if (cashboxDto != null)
            {
                builder.Append(cashboxDto.Name);
            }

            builder.Append($" {dto.Amount:f2} грн. от {dto.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)}");

            return builder.ToString();
        }

        private async Task<bool> RefundAmountWithTerminalAsync(IFiscalRegistrarClient fiscalRegistrarClient, IPosTerminalClient posTerminalClient)
        {
            CashboxDto terminalCashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(posTerminalClient.Settings.CashboxId));
            TerminalOptionsDto terminalOptions = await WebClient.ExecuteApiRequestAsync(new QueryTerminalOptions());

            OrderPaymentDto orderPaymentDto = orderPayments.FirstOrDefault(x => x.Id == Model.OrderPaymentId);

            ValidationResultItem[] errors = CanRefundWithTerminal().ToArray();

            if (errors.Any())
            {
                Logger.LogWarning("Failed refund with terminal");

                MessageFacadeService.ShowValidationResultView(
                    "Ошибки при выполнении возврата ДС",
                    errors.Where(x => x.IsError).ToArray(),
                    this);

                return false;
            }

            return ProcessTerminal(SendRequestPurchaseReturnToTerminalAsync);

            IEnumerable<ValidationResultItem> CanRefundWithTerminal()
            {
                if (!allowRefundTerminalMoney)
                {
                    yield return new ValidationResultItem("У пользователя нет прав для выполнения возврата через терминал", true);
                }

                if (terminalCashbox.LegalEntity?.White != false && fiscalRegistrarClient == null)
                {
                    yield return new ValidationResultItem("Не заполнены настройки для РРО", true);
                }

                if (orderPaymentDto == null)
                {
                    yield return new ValidationResultItem("Не выбрана транзакция", true);
                    yield break;
                }

                if (orderPaymentDto.Amount < Model.Amount)
                {
                    yield return new ValidationResultItem("Сумма возврата превышает сумму выбранной транзакции", true);
                }

                if (posTerminalClient.Settings.PosTypeId == PosTerminalType.IngenicoId && string.IsNullOrEmpty(orderPaymentDto.Rn))
                {
                    yield return new ValidationResultItem("У данной транзакции не заполнен RN.", true);
                }

                if (posTerminalClient.Settings.PosTypeId == PosTerminalType.UkrsibbankId && string.IsNullOrEmpty(orderPaymentDto.Rrn))
                {
                    yield return new ValidationResultItem("У данной транзакции не заполнен RRN.", true);
                }

                if (posTerminalClient.Settings.PosTypeId == PosTerminalType.UkrsibbankId && !orderPaymentDto.CheckNumber.HasValue)
                {
                    yield return new ValidationResultItem("У данной транзакции не заполнен номер чека.", true);
                }

                if (string.IsNullOrEmpty(orderPaymentDto.TerminalMacAddress))
                {
                    yield return new ValidationResultItem("Нет информации о мак-адресе выбранной транзакции", true);
                }

                if (terminalOptions?.CheckMacAddress != false && orderPaymentDto.TerminalMacAddress != posTerminalClient.Settings.MacAddress)
                {
                    yield return new ValidationResultItem($"Выполнять возврат можно только на том устройстве, с которого была внесена оплата, с Мак-адресом {orderPaymentDto.TerminalMacAddress}. Мак-адрес текущего устройства {posTerminalClient.Settings.MacAddress}", true);
                }
            }

            async Task<IEnumerable<ValidationResultItem>> SendRequestPurchaseReturnToTerminalAsync(IProgress<string> progress)
            {
                progress.Report("Связываемся с терминалом...");

                try
                {
                    Result<IPosTerminalClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                        _ => PosTerminalFactory.CreateAsync(Model.LegalEntity.Id),
                        "Определении настроек терминала",
                        null,
                        this,
                        true);

                    if (!clientResult.IsSuccess)
                    {
                        return clientResult.ErrorObj.GetMessages().DefaultIfEmpty("Необработанная ошибка при определении настроек терминала").Select(x => new ValidationResultItem(x, true));
                    }

                    bool returnFullAmountTransaction = orderPaymentDto?.Amount == Model.Amount;

                    Result<ReturnRefundResult> result = await clientResult.Data.ReturnRefundAsync(Model.Amount, returnFullAmountTransaction, clientResult.Data.Settings.Merchant, orderPaymentDto?.Rn, orderPaymentDto?.Rrn, orderPaymentDto?.CheckNumber, 0, orderPaymentDto?.CreatedOn, progress, CancellationToken.None);

                    _terminalRefundResult = result.Data;

                    if (!result.IsSuccess)
                    {
                        return result.ErrorObj.GetMessages().DefaultIfEmpty("Необработанная ошибка при возврате через терминал").Select(x => new ValidationResultItem(x, true));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to create payment in POS terminal");
                    return [new ValidationResultItem($"Ошибка при оплате через терминал: {ex.Message}", true)];
                }

                return [];
            }
        }

        private bool ProcessTerminal(Func<IProgress<string>, Task<IEnumerable<ValidationResultItem>>> action)
        {
            PreloaderViewModel preloaderViewModel = DialogDocumentManagerService.ShowView<PreloaderViewModel>(new PreloaderParameter(action, "Ошибки при возврате ДС через терминал"), this);

            return preloaderViewModel.IsOk;
        }

        private async Task<bool> CanRefundAmountAsync()
        {
            Result<object> returnResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CanRefund(Model.Id)),
                "при проверке возможности возврата ДС",
                "Проверка возможности возврата ДС",
                this,
                true);

            return returnResult is not null;
        }

        private async Task<ReadOnlyObservableCollection<ComboBoxItem>> GetOrderPaymentsTransactionsAsync()
        {
            IReadOnlyCollection<RefundDto> refund = await GetAllRefundOrdersForTransactionsAsync();

            ReadOnlyObservableCollection<ComboBoxItem> result = orderPayments
                .Where(x => CanShowTransaction(x, refund))
                .OrderBy(x => x.CreatedOn)
                .Select(y => new ComboBoxItem(y.Id, GetNameOrderPayment(y)))
                .ToReadOnlyObservableCollection();

            return result;
        }

        private async Task<IReadOnlyCollection<RefundDto>> GetAllRefundOrdersForTransactionsAsync()
        {
            IFilteringItem filteringItem = new RefundsFilteringItem
            {
                OrderIds = Model.OrderId.ToString(),
                Payments = new[] { Payment.TerminalId },
                States = new[] { RefundState.Done.Id }
            };

            PagedResult<RefundDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryRefunds(filteringItem)),
                "при получении данных о всех возвратах",
                string.Empty,
                this,
                false,
                showNotification: false);

            if (result is not null)
            {
                return result.Data;
            }

            return Array.Empty<RefundDto>();
        }

        private bool CanShowTransaction(OrderPaymentDto orderPaymentDto, IReadOnlyCollection<RefundDto> refund)
        {
            decimal amount = orderPaymentDto.Amount;

            if (refund != null && refund.Any(x => x.OrderPaymentId == orderPaymentDto.Id))
            {
                decimal sumRefund = refund.Where(x => x.OrderPaymentId == orderPaymentDto.Id).Select(y => y.Amount).Sum();

                amount = orderPaymentDto.Amount - sumRefund;
            }

            return amount >= Model.Amount && orderPaymentDto.PaymentId == Payment.TerminalId && orderPaymentDto.Sign > 0;
        }

        private void RefundUpdateRequisites(RefundUpdateRequisitesMessage message)
        {
            if (message.DocumentId != Model.Id || Model.EmployeeLockId is null)
            {
                return;
            }

            Model.FirstName = message.FirstName;
            Model.LastName = message.LastName;
            Model.MiddleName = message.MiddleName;
            Model.Iban = message.Iban;
            Model.CardNumber = message.CardNumber;
            Model.Inn = message.Inn;
        }
    }
}