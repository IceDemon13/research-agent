using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderCreditViewModel : TelemartDialogViewModelBase
    {
        private readonly IAsyncCommand _calculateCreditOffersCommand;
        private readonly IErrorHandler _errorHandler;
        private IReadOnlyCollection<CreditOfferDto> _allCreditOffers;
        private IReadOnlyCollection<ExternalPaymentDto> _actualExternalPayments;

        private OrderDto _order;
        private decimal _leftToPay;
        private int _priceTypeId;

        public OrderCreditViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _calculateCreditOffersCommand = new AsyncCommand(CalculateCreditOffersAsync);
        }

        public string ContractNumber
        {
            get { return GetProperty(() => ContractNumber); }
            set { SetProperty(() => ContractNumber, value); }
        }

        public string Info
        {
            get { return GetProperty(() => Info); }
            set { SetProperty(() => Info, value); }
        }

        public string OkButtonContent
        {
            get { return GetProperty(() => OkButtonContent); }
            set { SetProperty(() => OkButtonContent, value); }
        }

        public decimal? ContractAmount
        {
            get { return GetProperty(() => ContractAmount); }
            set { SetProperty(() => ContractAmount, value, OnContractAmountChanged); }
        }

        public int? CreditPartCount
        {
            get { return GetProperty(() => CreditPartCount); }
            set { SetProperty(() => CreditPartCount, value); }
        }

        public decimal? CreditPartCountAmount
        {
            get { return GetProperty(() => CreditPartCountAmount); }
            set { SetProperty(() => CreditPartCountAmount, value); }
        }

        public CreditOfferDto SelectedCreditOffer
        {
            get { return GetProperty(() => SelectedCreditOffer); }
            set { SetProperty(() => SelectedCreditOffer, value, OnCreditOfferChanged); }
        }

        public bool ReadOnlyContractAmount
        {
            get { return GetProperty(() => ReadOnlyContractAmount); }
            private set { SetProperty(() => ReadOnlyContractAmount, value); }
        }

        public bool ReadOnlyMode
        {
            get { return GetProperty(() => ReadOnlyMode); }
            private set { SetProperty(() => ReadOnlyMode, value); }
        }

        public DateTime? ContractDate
        {
            get { return GetProperty(() => ContractDate); }
            set { SetProperty(() => ContractDate, value); }
        }

        public Payment SelectedCreditPayment
        {
            get { return GetProperty(() => SelectedCreditPayment); }
            set { SetProperty(() => SelectedCreditPayment, value, OnSelectedCreditPaymentChanged); }
        }

        public ReadOnlyObservableCollection<Payment> CreditPayments
        {
            get { return GetProperty(() => CreditPayments); }
            private set { SetProperty(() => CreditPayments, value); }
        }

        public ReadOnlyObservableCollection<CreditOfferDto> CreditOffers
        {
            get { return GetProperty(() => CreditOffers); }
            private set { SetProperty(() => CreditOffers, value); }
        }

        public OrderCreditDataDto CreditData
        {
            get { return GetProperty(() => CreditData); }
            private set { SetProperty(() => CreditData, value); }
        }

        public bool CreditOffersEnabled => !ReadOnlyMode && CreditOffers?.Any() == true;

        public bool ContractDateVisible => SelectedCreditPayment?.Id == Payment.AlfabankId || SelectedCreditPayment?.Id == Payment.PaylaterId;

        public bool ContractNumberVisible => SelectedCreditPayment?.Id == Payment.AlfabankId || SelectedCreditPayment?.Id == Payment.PaylaterId;

        public bool ContractDateRequired => SelectedCreditPayment?.Id == Payment.AlfabankId && !ReadOnlyMode;

        public bool ContractNumberRequired => SelectedCreditPayment?.Id == Payment.AlfabankId && !ReadOnlyMode;

        public static void BuildMetadata(MetadataBuilder<OrderCreditViewModel> builder)
        {
            builder.Property(x => x.ContractDate)
                .MatchesInstanceRule((x, y) => !y.ContractDateRequired || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCreditOffer)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCreditPayment)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ContractAmount)
                .MatchesInstanceRule(
                    (x, y) => y.SelectedCreditPayment is null || y.ReadOnlyContractAmount || (y.SelectedCreditPayment != null
                                                                  && (y.SelectedCreditPayment.MinLimitUah is null || x >= y.SelectedCreditPayment.MinLimitUah)
                                                                  && x <= Math.Min(y.SelectedCreditPayment.LimitUah, y._leftToPay)),
                    (_, y) => $"Минимально допустимая сумма по выбранному способу оплаты: {y.SelectedCreditPayment.MinLimitUah}\nМаксимально допустимая сумма по выбранному способу оплаты: {y.SelectedCreditPayment.LimitUah}\nОстаток к оплате по заказу: {y._leftToPay}");
            builder.Property(x => x.ContractNumber)
                .MatchesInstanceRule((x, y) => y.SelectedCreditPayment != null && (!y.ContractNumberRequired || x is { Length: > 7 and < 13 }), () => "Стандартная длина договора 9 символов (также допустимо от 8 до 12)");
        }

        protected override async Task HandleLoadedAsync()
        {
            _allCreditOffers = await WebClient.ExecuteApiRequestAsync(new QueryCreditOffers());

            OrderCreditParameter parameter = (OrderCreditParameter)Parameter;

            _order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

            _leftToPay = _order.GetLeftToPay().Uah;
            _priceTypeId = parameter.PriceTypeId;
            _actualExternalPayments = _order.ExternalPayments?.Where(x => PaymentState.IsActual(x.PaymentStateId)).ToList();

            if (_leftToPay > 0)
            {
                ContractAmount = _leftToPay;
            }

            HashSet<int> paymentIds = _allCreditOffers.Select(x => x.PaymentId).ToHashSet();

            CreditPayments = paymentIds
                .Select(x => Dictionaries.GetItemById<Payment>(x))
                .Where(x => x.Active && (_order.PaymentId == x.Id || (_order.PaymentId == Payment.CashId && x.PartialCredit) || _actualExternalPayments.Any(z => x.Id == z.PaymentId)))
                .ToReadOnlyObservableCollection();

            if (!CreditPayments.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет доспутных способов оплаты кредита");
            }

            ExternalPaymentDto externalPayment = _actualExternalPayments?.FirstOrDefault(x => x.Params?.CreditOfferId > 0);

            if (externalPayment is null)
            {
                if (_allCreditOffers.Any(x => x.PaymentId == _order.PaymentId))
                {
                    SelectedCreditPayment = Dictionaries.GetItemById<Payment>(_order.PaymentId);
                }
            }
            else
            {
                SelectedCreditPayment = Dictionaries.GetItemById<Payment>(externalPayment.PaymentId);
            }

            Title = $"Кредиты по заказу №{_order.Id}";

            RaisePropertiesChanged(
                nameof(ContractDate),
                nameof(SelectedCreditOffer),
                nameof(ContractAmount),
                nameof(ContractNumber));

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedCreditPayment!.Id == Payment.AlfabankId
                && ContractNumber.Length != 9
                && !MessageFacadeService.Confirm("Вы указали длину номера договора отличную от 9 символов. Вы уверены?"))
            {
                return;
            }

            CreditData = new OrderCreditDataDto(ContractNumber, ContractDate, ContractAmount!.Value, SelectedCreditOffer!.Id);

            (await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreditOrder(_order.Id, CreditData)),
                "сохранении кредита",
                "Кредит сохранен",
                this,
                true)).IfNotNull(_ => { CloseOk(); });
        }

        private void OnSelectedCreditPaymentChanged()
        {
            OkButtonContent = "Сформировать кредит";

            if (SelectedCreditPayment is null)
            {
                ClearFields();

                ReadOnlyMode = true;
                ReadOnlyContractAmount = true;
                Info = "Выберите способ оплаты";
            }
            else
            {
                ExternalPaymentDto externalPayment = _actualExternalPayments?.FirstOrDefault(x => x.PaymentId == SelectedCreditPayment.Id);

                bool externalPaymentIsActual = externalPayment is not null && PaymentState.IsActual(externalPayment.PaymentStateId);

                if (externalPayment is not null)
                {
                    CreditOffers = _allCreditOffers
                        .Where(x => x.PaymentId == SelectedCreditPayment!.Id)
                        .OrderBy(x => x.Month)
                        .ToReadOnlyObservableCollection();

                    if (externalPayment.PaymentId == Payment.PaylaterId)
                    {
                        OkButtonContent = "Сохранить";
                    }
                }

                ReadOnlyMode = _order.StateId != OrderStatus.Received.Id || (externalPaymentIsActual && externalPayment.PaymentId != Payment.PaylaterId);
                ReadOnlyContractAmount = _order.StateId != OrderStatus.Received.Id || externalPaymentIsActual;

                if (externalPaymentIsActual)
                {
                    ContractDate = externalPayment.Params?.ContractDate;
                    SelectedCreditOffer = _allCreditOffers.FirstOrDefault(x => x.Id == externalPayment.Params?.CreditOfferId);
                    ContractAmount = externalPayment.CreatedAmount;
                    ContractNumber = externalPayment.Params?.ContractNumber;
                    Info = "Кредит сформирован";
                }
                else if (_order.StateId != OrderStatus.Received.Id)
                {
                    Info = "Создание доступно для заказов в статусе 'Принят'";
                }
                else
                {
                    Info = null;
                    ContractAmount = _leftToPay;
                    _calculateCreditOffersCommand.Execute(null);
                }
            }

            RaisePropertiesChanged(
                nameof(ContractDate),
                nameof(ContractNumber),
                nameof(ContractAmount),
                nameof(ContractDateRequired),
                nameof(ContractNumberRequired),
                nameof(CreditOffersEnabled),
                nameof(ContractDateVisible),
                nameof(ContractNumberVisible));
        }

        private void OnContractAmountChanged()
        {
            CalculateMonthlyContractAmount();
        }

        private void OnCreditOfferChanged()
        {
            CalculateMonthlyContractAmount();
        }

        private void CalculateMonthlyContractAmount()
        {
            if (SelectedCreditOffer is not null && ContractAmount.HasValue)
            {
                CreditPartCount = SelectedCreditOffer.CreditPartCount;
                CreditPartCountAmount = Math.Round(ContractAmount.Value / SelectedCreditOffer.CreditPartCount, 2);
            }
            else
            {
                CreditPartCountAmount = null;
            }
        }

        private async Task CalculateCreditOffersAsync()
        {
            if (SelectedCreditPayment is null)
            {
                return;
            }

            _allCreditOffers ??= await WebClient.ExecuteApiRequestAsync(new QueryCreditOffers());

            List<(int productId, string productName, int? partialPay)> productPartialPays = _order.Products
                .Where(x => x.ProductTypeId != ProductType.GuestProductId)
                .Select(x => (x.Product.Id, x.Product.Name, GetPartialPayByPaymentId(x.Product.Prices.FirstOrDefault(z => z.PriceTypeId == _priceTypeId))))
                .ToList();

            int? maxCreditPartCount = productPartialPays
                .Select(x => x.partialPay)
                .DefaultIfEmpty(0)
                .Min() ?? 0;

            List<ValidationResultItem> errors = productPartialPays
                .Where(x => x.partialPay is null)
                .Select(x => new ValidationResultItem($"Товар {x.productName} ({x.productId}) не поддерживает выбранное кредитование", true))
                .ToList();

            if (errors.Any())
            {
                ShowValidationResultView("Ошибка", errors);
                ClearFields();

                return;
            }

            if (maxCreditPartCount is null or 0)
            {
                MessageFacadeService.ShowNotificationError("Товары не поддерживают выбранное кредитование");
                ClearFields();

                return;
            }

            CreditOffers = _allCreditOffers
                .Where(x => x.PaymentId == SelectedCreditPayment.Id && x.Active && x.CreditPartCount <= maxCreditPartCount)
                .OrderBy(x => x.Month)
                .ToReadOnlyObservableCollection();

            if (!CreditOffers.Any() && SelectedCreditOffer is null)
            {
                MessageFacadeService.ShowNotificationWarning("Нет доступных кредитных предложений");
            }
        }

        private int? GetPartialPayByPaymentId(ProductPriceSimpleDto productPriceSimpleDto)
        {
            const int MaxMonth = 24;

            return SelectedCreditPayment.Id switch
            {
                Payment.PrivatPartialPayId => productPriceSimpleDto?.PartialPayPb,
                Payment.CreditId => productPriceSimpleDto?.PartialPayPb,
                Payment.PumbId => productPriceSimpleDto?.PartialPayPumb,
                Payment.ABankId => productPriceSimpleDto?.PartialPayAb,
                Payment.MonobankId => productPriceSimpleDto?.PartialPay,
                Payment.AlfabankId => MaxMonth,
                Payment.PaylaterId => MaxMonth,
                _ => null
            };
        }

        private void ClearFields()
        {
            ContractDate = null;
            SelectedCreditOffer = null;
            ContractAmount = _leftToPay;
            ContractNumber = null;
            SelectedCreditPayment = null;
        }
    }
}