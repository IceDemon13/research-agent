using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.PosTerminal;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.TransferObjects.Terminal;
using Telemart.Client.Validators;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;
using ProductType = Telemart.Common.Dictionaries.ProductType;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddOrderPaymentViaCashRegistrarViewModel : TelemartDialogViewModelBase
    {
        private readonly IFiscalRegistrarClientFactory _fiscalRegistrarClientFactory;
        private readonly IPosTerminalFactory _posTerminalFactory;
        private readonly IErrorHandler _errorHandler;
        private readonly IFiscalSettingsValidator _fiscalSettingsValidator;
        private readonly IPosSettingsValidator _posSettingsValidator;
        private readonly IMessenger _messenger;
        private readonly IEquipmentSettingsStore _equipmentSettingsStore;
        private readonly IMapper _mapper;

        private int _orderId;
        private bool _printCheque;
        private bool _prepayment;
        private TerminalDataDto _terminalData;
        private IReadOnlyCollection<CashboxDto> _cashboxes;
        private IReadOnlyCollection<PosSettingsDto> _terminals;
        private string _fiscalId;
        private int _sellCashboxId;

        public AddOrderPaymentViaCashRegistrarViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IPosTerminalFactory posTerminalFactory,
            IErrorHandler errorHandler,
            IFiscalSettingsValidator fiscalSettingsValidator,
            IPosSettingsValidator posSettingsValidator,
            IEquipmentSettingsStore equipmentSettingsStore,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _fiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            _posTerminalFactory = posTerminalFactory;
            _errorHandler = errorHandler;
            _fiscalSettingsValidator = fiscalSettingsValidator;
            _posSettingsValidator = posSettingsValidator;
            _equipmentSettingsStore = equipmentSettingsStore;
            _messenger = messenger;
            _mapper = mapper;

            WithoutTerminalCommand = new AsyncCommand(AddPaymentWithoutTerminalAsync);
        }

        public AddOrderPaymentViaCashRegistrarViewModel()
        {
        }

        public IAsyncCommand WithoutTerminalCommand { get; }

        public OrderDto Order { get; private set; }

        public OrderPaymentDto OrderPayment { get; private set; }

        public IFiscalRegistrarClient FiscalRegistrarClient { get; private set; }

        #region INPC

        public CashboxDto SelectedCashbox
        {
            get { return GetProperty(() => SelectedCashbox); }
            set { SetProperty(() => SelectedCashbox, value); }
        }

        public Payment SelectedPayment
        {
            get { return GetProperty(() => SelectedPayment); }
            set { SetProperty(() => SelectedPayment, value, PaymentChanged); }
        }

        public decimal ToPayAmount
        {
            get { return GetProperty(() => ToPayAmount); }
            set { SetProperty(() => ToPayAmount, value, () => { RaisePropertiesChanged(nameof(PayedAmount), nameof(ShortChange)); }); }
        }

        public decimal PayedAmount
        {
            get { return GetProperty(() => PayedAmount); }
            set { SetProperty(() => PayedAmount, value, () => { RaisePropertiesChanged(nameof(ShortChange), nameof(Amount)); }); }
        }

        public decimal Amount => PayedAmount - ShortChange;

        public decimal ShortChange => Math.Max(0, PayedAmount - ToPayAmount);

        public bool CanAddWithoutTerminal => SelectedPayment?.Id == Payment.TerminalId;

        public bool FullAmount
        {
            get { return GetProperty(() => FullAmount); }
            private set { SetProperty(() => FullAmount, value, () => { RaisePropertiesChanged(nameof(PayedAmount)); }); }
        }

        public ReadOnlyObservableCollection<CashboxDto> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ReadOnlyObservableCollection<Payment> PaymentTypes
        {
            get { return GetProperty(() => PaymentTypes); }
            private set { SetProperty(() => PaymentTypes, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<AddOrderPaymentViaCashRegistrarViewModel> builder)
        {
            builder.Property(x => x.SelectedCashbox).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedPayment).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.PayedAmount).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.PayedAmount).MatchesInstanceRule((x, y) => y.FullAmount == false || x >= y.ToPayAmount, () => "Значение не может быть меньше чем сумма к оплате");
            builder.Property(x => x.PayedAmount).MatchesInstanceRule((x, y) => y.SelectedPayment?.Id == Payment.CashId || x <= y.ToPayAmount, () => "Значение не может быть больше чем сумма к оплате");
            builder.Property(x => x.PayedAmount).MatchesInstanceRule((x, y) => y.SelectedPayment?.Id != Payment.CashId || x <= FiscalRegistrarConstants.FiscalRegistrarDayLimit, () => $"Значение не может быть больше чем {FiscalRegistrarConstants.FiscalRegistrarDayLimit} грн.");
        }

        public string FiscalId => _fiscalId;

        public int SellCashboxId => _sellCashboxId;

        protected override async Task HandleLoadedAsync()
        {
            AddOrderPaymentViaCashRegistrarParameter parameter = (AddOrderPaymentViaCashRegistrarParameter)Parameter;

            _orderId = parameter.Order.Id;
            _printCheque = parameter.PrintCheque;
            _prepayment = parameter.Prepayment;

            decimal totalCost = parameter.Order.GetOrderProducts().Where(x => x.Price.CurrencyId == Currency.Uah.Id).Sum(x => x.Price.Value * x.Quantity);
            decimal deliveryCost = parameter.Order.PackageDeliveryCost ?? 0;
            decimal prepayment = parameter.Order.GetOrderPayments().Where(x => x.CurrencyId == Currency.Uah.Id).Sum(x => x.Sign * x.Amount);

            ToPayAmount = totalCost + deliveryCost - prepayment;
            PayedAmount = parameter.Amount ?? ToPayAmount;
            FullAmount = parameter.FullAmount;

            EquipmentSettingsInfo equipmentSettings = await _equipmentSettingsStore.LoadAsync();

            Guid uniqueDeviceGuid = equipmentSettings.UniqueDeviceGuid!.Value;

            List<PosSettingsDto> posSettings = await WebClient.ExecuteApiRequestAsync(new QueryPosSettings(uniqueDeviceGuid.ToString()));

            Result<IReadOnlyCollection<PosSettingsDto>> posSettingsValidationResult = await _posSettingsValidator.ValidateAsync(posSettings);

            List<ValidationResultItem> errors = new List<ValidationResultItem>();

            if (!posSettingsValidationResult.IsSuccess)
            {
                errors.AddRange(posSettingsValidationResult.ErrorObj.GetMessages()
                        .Select(x => new ValidationResultItem(x, false)));
            }
            else if (posSettingsValidationResult.Warnings?.Any() == true)
            {
                errors.AddRange(posSettingsValidationResult.Warnings
                        .Select(x => new ValidationResultItem(x, false)));
            }

            if (parameter.OrderLegalEntity?.White == false && posSettingsValidationResult.Data?.Any(x => x.LegalEntityId == parameter.OrderLegalEntity.Id) != true)
            {
                MessageFacadeService.ShowNotificationWarning("Юр. лицо в заказе не использует РРО");
                Close();
            }

            List<int> paymentIds = new List<int>();
            List<int> cashboxIds = new List<int>();

            if (parameter.OrderLegalEntity?.White != false)
            {
                List<FiscalRegistrarSettingsDto> fiscalRegistrarSettings = await WebClient.ExecuteApiRequestAsync(new QueryFiscalRegistrarSettings(uniqueDeviceGuid.ToString(), true));

                fiscalRegistrarSettings = fiscalRegistrarSettings
                    .Where(x => parameter.OrderLegalEntity == null || parameter.OrderLegalEntity.Id == x.LegalEntityId)
                    .ToList();

                if (!fiscalRegistrarSettings.Any())
                {
                    string message = parameter.OrderLegalEntity == null
                        ? "Нет активных настроек РРО"
                        : $"Выберите или добавьте, в настройках, РРО с юр. лицом \"{parameter.OrderLegalEntity!.Name}\"";

                    MessageFacadeService.ShowNotificationWarning(message);
                    Close();
                }

                Result<IReadOnlyCollection<FiscalRegistrarSettingsDto>> fiscalValidationResult = await _fiscalSettingsValidator.ValidateAsync(fiscalRegistrarSettings);

                if (!fiscalValidationResult.IsSuccess)
                {
                    errors.AddRange(fiscalValidationResult.ErrorObj.GetMessages()
                            .Select(x => new ValidationResultItem(x, false)));
                }
                else if (fiscalValidationResult.Warnings?.Any() == true)
                {
                    errors.AddRange(fiscalValidationResult.Warnings
                            .Select(x => new ValidationResultItem(x, false)));
                }

                if (fiscalValidationResult.IsSuccess)
                {
                    if (fiscalValidationResult.Data.Count > 1)
                    {
                        MessageFacadeService.ShowNotificationWarning("Найдено более одной настройки РРО");
                    }
                    else
                    {
                        FiscalRegistrarClient = await _fiscalRegistrarClientFactory.CreateAsync(fiscalValidationResult.Data.First(), CancellationToken.None);
                        paymentIds.Add(Payment.CashId);
                        cashboxIds.Add(FiscalRegistrarClient.Settings.CashboxId);
                    }
                }
            }

            if (errors.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки терминалов и РРО", errors, this);
            }

            List<LegalEntityDto> legalEntities = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

            int[] greyLegalEntityIds = legalEntities.Where(x => x.White == false).Select(x => x.Id).ToArray();

            _terminals = (posSettingsValidationResult.Data ?? Enumerable.Empty<PosSettingsDto>())
                .Where(x => (parameter.OrderLegalEntity == null || x.LegalEntityId == parameter.OrderLegalEntity.Id)
                    && (x.LegalEntityId == FiscalRegistrarClient?.Settings?.LegalEntityId || greyLegalEntityIds.Contains(x.LegalEntityId ?? 0)))
                .ToArray();

            if (_terminals.Count > 0)
            {
                paymentIds.Add(Payment.TerminalId);

                cashboxIds.AddRange(_terminals.Select(x => x.CashboxId));
            }

            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            _cashboxes = cashboxes
                .Where(x => WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id) && cashboxIds.Contains(x.Id))
                .ToArray();

            PaymentTypes = Dictionaries.GetItems<Payment>()
                .Where(x => paymentIds.Contains(x.Id))
                .ToReadOnlyObservableCollection();

            if (PaymentTypes.Count == 1)
            {
                SelectedPayment = PaymentTypes.First();
            }

            Title = "Выберите кассу";
        }

        protected override Task HandleOkAsync()
        {
            return AddPaymentAsync(true);
        }

        private Task AddPaymentWithoutTerminalAsync()
        {
            if (MessageFacadeService.Confirm("Будут внесены денежные средства без терминала. Вы уверены?"))
            {
                return AddPaymentAsync(false);
            }

            return Task.CompletedTask;
        }

        private async Task AddPaymentAsync(bool withTerminal)
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            bool skipRro = SelectedPayment.Id == Payment.TerminalId
                           && SelectedCashbox.LegalEntity.White == false;

            if (skipRro == false)
            {
                bool valid = await ValidateRroAndLoadCashInCashboxAsync();

                if (!valid)
                {
                    return;
                }
            }

            if (!SelectedCashbox.AllowedPayments.Contains(SelectedPayment.Id))
            {
                MessageFacadeService.ShowMessageBoxError("Выбранная касса доступна только для: " + string.Join(", ", SelectedCashbox.AllowedPayments.Select(x => Dictionaries.GetItemById<Payment>(x).Name)));
                return;
            }

            PosSettingsDto terminal = _terminals.FirstOrDefault(x => x.CashboxId == SelectedCashbox.Id);

            if (SelectedPayment.Id == Payment.TerminalId && withTerminal)
            {
                if (terminal == null)
                {
                    MessageFacadeService.ShowNotificationWarning("Не заполнены настройки для терминала");
                    return;
                }

                if (skipRro == false && terminal.LegalEntityId != FiscalRegistrarClient.Settings.LegalEntityId)
                {
                    MessageFacadeService.ShowNotificationWarning("Юр. лицо терминала не соответствует юр. лицу выбранного РРО");
                    return;
                }

                PreloaderViewModel preloaderViewModel = DialogDocumentManagerService.ShowView<PreloaderViewModel>(new PreloaderParameter(SendRequestToTerminalAsync, "Ошибки при оплате через терминал", "Retrieving the COM class factory for component with CLSID"), this);

                if (!preloaderViewModel.IsOk)
                {
                    if (preloaderViewModel.IsSpecialError)
                    {
                        await InstallationPosComObjectAsync();
                    }

                    return;
                }
            }

            await AddOrderPaymentAsync(skipRro);

            IsOk = true;
            Close();

            async Task<IEnumerable<ValidationResultItem>> SendRequestToTerminalAsync(IProgress<string> progress)
            {
                progress.Report("Связываемся с терминалом...");

                try
                {
                    IPosTerminalClient client = await _posTerminalFactory.CreateAsync(terminal);

                    Result<TerminalPurchaseResult> result = await client.PurchaseAsync(Amount, 0, terminal.Merchant, progress, CancellationToken.None);

                    if (result.IsSuccess)
                    {
                        if (result.Data != null)
                        {
                            _terminalData = _mapper.Map<TerminalDataDto>(result.Data);
                            _terminalData.TerminalMacAddress = client.Settings.MacAddress;
                        }
                    }
                    else
                    {
                        IEnumerable<string> errors = result.ErrorObj.GetMessages();

                        return errors.Select(x => new ValidationResultItem(x, true));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to create payment in POS terminal");

                    return new[] { new ValidationResultItem($"Ошибка при оплате через терминал: {ex.Message}", true) };
                }

                return Array.Empty<ValidationResultItem>();
            }

            Task InstallationPosComObjectAsync()
            {
                if (MessageFacadeService.Confirm("На данном устройстве не установлен COM-объект для работы с терминалами. Установить необходимое ПО?", "Ошибка при оплате через терминал"))
                {
                    return WebClient.InstallationPosComObjectAsync();
                }

                return Task.CompletedTask;
            }
        }

        private async Task<bool> ValidateRroAndLoadCashInCashboxAsync()
        {
            if (FiscalRegistrarClient == null)
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнены настройки для РРО");
                return false;
            }

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(FiscalRegistrarClient.Settings.CashboxId));

            if (cashbox.Session?.Closed != false)
            {
                MessageFacadeService.ShowNotificationWarning("Смена закрыта");
                return false;
            }

            bool canAddOrderPayment = await CanAddOrderPaymentViaCashRegistrarInternalAsync(_orderId, cashbox.Id);

            if (!canAddOrderPayment)
            {
                return false;
            }

            decimal? inCashbox = null;

            await _errorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClient.GetCashStockAsync(
                    FiscalRegistrarClient.Settings.CashboxId,
                    ct),
                "запросе остатка по кассе из РРО",
                "Запрос остатка суммы в кассе выполнен",
                this,
                true,
                onSuccess: (r, _) =>
                {
                    inCashbox = r;

                    return Task.CompletedTask;
                });

            if (inCashbox == null)
            {
                return false;
            }

            if (ShortChange > inCashbox)
            {
                MessageFacadeService.ShowNotificationWarning("В кассе недостаточно средств для cдачи");
                return false;
            }

            return true;
        }

        private async Task<bool> CanAddOrderPaymentViaCashRegistrarInternalAsync(int orderId, int cashboxId)
        {
            IReadOnlyCollection<ValidationResultItem> errorResult = null;

            try
            {
                Result<OrderDto> canPayResult = await WebClient.ExecuteApiRequestAsync(new CanPayOrderViaCashRegistrar(orderId, cashboxId));

                if (canPayResult.Warnings.Any())
                {
                    errorResult = canPayResult.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray();
                }
            }
            catch (UnexpectedSatusException exception)
            {
                errorResult = exception.GetErrorItems();
            }
            catch (UnexpectedErrorException)
            {
                errorResult = new[] { new ValidationResultItem(Resources.ServerUnavailable, true) };
            }
            catch (Exception)
            {
                errorResult = new[] { new ValidationResultItem("Ошибка при проверке возможности внесения оплаты", true) };
            }

            if (errorResult != null && errorResult.Any())
            {
                MessageFacadeService.ShowValidationResultView("Ошибки", errorResult, this);
                return false;
            }

            return true;
        }

        private async Task AddOrderPaymentAsync(bool skipRro)
        {
            try
            {
                CreateOrderPayment gatewayRequest = new CreateOrderPayment(
                    _orderId,
                    SelectedCashbox.CurrencyId,
                    SelectedPayment.Id,
                    SelectedCashbox.Id,
                    Amount,
                    DateTime.Today,
                    null,
                    _terminalData,
                    fiscalCahboxId: FiscalRegistrarClient?.Settings?.CashboxId,
                    prepayment: _prepayment);

                Result<OrderPaymentResultDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                OrderPayment = result.Data.OrderPayment;
                Order = result.Data.Order;

                if (skipRro)
                {
                    Result<OrderPaymentDto> orderPaymentResult = await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(result.Data.OrderPayment.Id, null, true, SelectedCashbox.Id));

                    OrderPayment = orderPaymentResult.Data;
                }
                else
                {
                    if (_printCheque)
                    {
                        await PrintOrderPaymentFiscalChequeAsync();
                    }
                }

                MessageFacadeService.ShowNotificationInfo($"Данные об оплате для заказа №{result.Data.Order.Id} успешно сохранены");

                _messenger.Send(new OrderPaymentMessage(OrderPayment, MessageType.Added));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки при внесении оплаты заказа", exception.GetErrorItems(), this);
            }
            catch (UnexpectedErrorException)
            {
               MessageFacadeService.ShowValidationResultView(
                    "Ошибки при внесении оплаты заказа",
                    new[] { new ValidationResultItem(Resources.ServerUnavailable, true) },
                    this);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while creating order payment");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private async Task PrintOrderPaymentFiscalChequeAsync()
        {
            if (FiscalRegistrarClient == null)
            {
                MessageFacadeService.ShowNotificationWarning("Не заполнены настройки для РРО");
                return;
            }

            Payment payment = Dictionaries.GetItemById<Payment>(Order.PaymentId);

            if (!payment.Fiscal)
            {
                await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(OrderPayment.Id, string.Empty, true, FiscalRegistrarClient.Settings.CashboxId));

                return;
            }

            SellRequest sellRequest = new SellRequest(
                Guid.NewGuid().ToString(),
                FiscalRegistrarClient.Settings.CashboxId,
                $"Касир: {WebClient.AuthenticatedEmployee.ShortName}",
                $"Замовлення №{Order.Id}",
                Order.Id,
                new[] { new SellProductRequest(FiscalRegistrarConstants.PrepaymentProductId, "Передплата", null, OrderPayment.Amount, 1, ProductType.Product) },
                new[]
                {
                    new SellPaymentRequest(FiscalPaymentType.GetByPaymentId(SelectedPayment.Id), OrderPayment.Amount)
                    {
                        CheckNumber = _terminalData?.CheckNumber,
                        Rrn = _terminalData?.Rrn,
                        TerminalId = _terminalData?.TerminalId,
                        MerchantId = _terminalData?.MerchantId,
                        AuthCode = _terminalData?.AuthCode,
                        Pan = _terminalData?.Pan,
                        IssuerName = _terminalData?.IssuerName
                    }
                },
                Array.Empty<string>(),
                null);

            await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(OrderPayment.Id, sellRequest.OperationId, payment.Fiscal, FiscalRegistrarClient.Settings.CashboxId));

            PreloaderViewModel model = DialogDocumentManagerService.ShowView<PreloaderViewModel>(
                new PreloaderParameter(x => FiscalRegistrarSellAsync(FiscalRegistrarClient, sellRequest, WebClient, x), canClose: false),
                this);

            if (model.IsOk)
            {
                MessageFacadeService.ShowNotificationInfo("Оплата в РРО проведена успешно.");
            }

            async Task<IEnumerable<ValidationResultItem>> FiscalRegistrarSellAsync(IFiscalRegistrarClient fiscalRegistrarClient, SellRequest request, IWebClient webClient, IProgress<string> progress)
            {
                progress.Report("Проведение оплаты через РРО. Не закрывайте окно.");

                try
                {
                    await fiscalRegistrarClient.SellAsync(request, default);
                }
                catch { }

                Result<QueryReceiptStatusResponse> response = await _errorHandler.HandleErrorsAsync(
                    ct => fiscalRegistrarClient.GetReceiptStatusAsync(sellRequest.CashboxId, request.OperationId, ct),
                    "проведении оплаты через РРО",
                    null,
                    this,
                    false,
                    showError: false,
                    showNotification: false,
                    showDialog: false,
                    onSuccess: async (x, _) =>
                    {
                        Result<QueryReceiptStatusResponse> statusResult = await fiscalRegistrarClient.GetReceiptStatusAsync(sellRequest.CashboxId, request.OperationId, default);

                        if (x.Data.Status == "ERROR")
                        {
                            MessageFacadeService.ShowNotificationInfo("Ошибка при проверке статуса чека");

                            OrderPayment = (await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(OrderPayment.Id, string.Empty, false, FiscalRegistrarClient.Settings.CashboxId))).Data;

                            return;
                        }

                        progress.Report("Оплата в РРО проведена успешно.");

                        Result<OrderPaymentDto> result = await webClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(OrderPayment.Id, request.OperationId, false, sellRequest.CashboxId));

                        OrderPayment = result.Data;

                        _fiscalId = request.OperationId;
                        _sellCashboxId = sellRequest.CashboxId;

                        // TODO: Change fiscal document number to string
                        await webClient.ExecuteApiRequestAsync(new CreateFiscalDocument(request.ToDocument(fiscalRegistrarClient.Settings.CashboxId, request.OperationId, Order.Id, Entity.OrderId)));

                        progress.Report("Подтверждение оплаты успешно.");
                    },
                    onError: async (ex, ct) =>
                    {
                        OrderPayment = (await WebClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(OrderPayment.Id, string.Empty, false, FiscalRegistrarClient.Settings.CashboxId))).Data;
                    });

                return Array.Empty<ValidationResultItem>();
            }
        }

        private void PaymentChanged()
        {
            if (SelectedPayment == null)
            {
                SelectedCashbox = null;
            }
            else if (SelectedPayment.Id == Payment.CashId)
            {
                Cashboxes = _cashboxes
                    .Where(x => FiscalRegistrarClient?.Settings?.CashboxId == x.Id)
                    .ToReadOnlyObservableCollection();
            }
            else if (SelectedPayment.Id == Payment.TerminalId)
            {
                Cashboxes = _cashboxes
                    .Where(x => _terminals.Any(y => y.CashboxId == x.Id))
                    .ToReadOnlyObservableCollection();
            }

            if (Cashboxes.Count == 1)
            {
                SelectedCashbox = Cashboxes.First();
            }

            RaisePropertiesChanged(nameof(PayedAmount), nameof(CanAddWithoutTerminal));
        }
    }
}