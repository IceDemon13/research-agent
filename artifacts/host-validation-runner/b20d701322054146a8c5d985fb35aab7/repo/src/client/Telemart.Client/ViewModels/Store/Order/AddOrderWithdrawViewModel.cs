using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddOrderWithdrawViewModel : TelemartDialogViewModelBase
    {
        private List<CashboxDto> _cashboxes;
        private int _orderId;
        private string _firstName;
        private string _lastName;
        private string _middleName;

        private AddOrderWithdrawParameter parameter;

        public AddOrderWithdrawViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMapper mapper,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;
        }

        public AddOrderWithdrawViewModel()
        {
        }

        #region INPC

        public Payment SelectedPaymentType
        {
            get { return GetProperty(() => SelectedPaymentType); }
            set { SetProperty(() => SelectedPaymentType, value, SelectedCurrencyOrPaymentChanged); }
        }

        public Currency SelectedCurrency
        {
            get { return GetProperty(() => SelectedCurrency); }
            set { SetProperty(() => SelectedCurrency, value, SelectedCurrencyOrPaymentChanged); }
        }

        public int? SelectedCashboxId
        {
            get { return GetProperty(() => SelectedCashboxId); }
            set { SetProperty(() => SelectedCashboxId, value); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public bool Readonly
        {
            get { return GetProperty(() => Readonly); }
            set { SetProperty(() => Readonly, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            private set { SetProperty(() => Requisites, value); }
        }

        public ObservableCollection<CashboxDto> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ObservableCollection<Payment> PaymentTypes
        {
            get { return GetProperty(() => PaymentTypes); }
            private set { SetProperty(() => PaymentTypes, value); }
        }

        #endregion

        public Result<OrderWithdrawResultDto> Result { get; private set; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<AddOrderWithdrawViewModel> builder)
        {
            builder.Property(x => x.SelectedCurrency)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedCashboxId)
                .MatchesInstanceRule((x, y) => y.SelectedPaymentType?.Id != Payment.CashId || x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Amount)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Comment)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (AddOrderWithdrawParameter)Parameter;

            _orderId = parameter.OrderId;
            _firstName = parameter.FirstName;
            _lastName = parameter.LastName;
            _middleName = parameter.MiddleName;

            Amount = parameter.Amount;
            Readonly = parameter.ReadOnly;

            _cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            Currencies = parameter.OrderPayments.Any()
                ? parameter.OrderPayments.Select(x => x.CurrencyId).Distinct().Select(Currency.GetById).ToObservableCollection()
                : new ObservableCollection<Currency> { Currency.Uah, Currency.Usd };

            int[] orderPaymentTypeIds = parameter.OrderPayments
                .Where(x => x.Sign > 0 && x.PaymentId.HasValue)
                .Select(x => x.PaymentId.Value)
                .Distinct()
                .ToArray();

            ObservableCollection<Payment> paymentTypes = Dictionaries
                .GetRefundPayments(orderPaymentTypeIds)
                .Where(x => x.Active || x.Id == parameter.PaymentTypeId)
                .OrderBy(x => x.Id)
                .ToObservableCollection();

            if (!paymentTypes.Any())
            {
                paymentTypes.AddRange(Dictionaries.GetItems<Payment>().Where(x => x.Id == Payment.CashId || x.Id == Payment.BankId));
            }

            PaymentTypes = paymentTypes;

            if (parameter.CurrencyId.HasValue)
            {
                SelectedCurrency = Currencies.FirstOrDefault(x => x.Id == parameter.CurrencyId.Value);
            }

            if (parameter.PaymentTypeId.HasValue)
            {
                SelectedPaymentType = PaymentTypes.FirstOrDefault(x => x.Id == parameter.PaymentTypeId);
            }

            SelectedCashboxId = parameter.CashboxId;

            Title = "Возврат ДС";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                CreateOrderWithdraw gatewayRequest = new CreateOrderWithdraw(
                    _orderId,
                    SelectedPaymentType?.Id == Payment.CashId ? SelectedCashboxId : null,
                    SelectedPaymentType?.Id,
                    SelectedCurrency.Id,
                    Amount,
                    Comment,
                    false,
                    Mapper.Map<RefundRequisitesDto>(Requisites));

                Result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Данные о возврате ДС для заказа №{_orderId} успешно сохранены");

                Messenger.Send(new OrderPaymentMessage(Result.Data.OrderPayment, MessageType.Added));
                Messenger.Send(new RefundMessage(Result.Data.MoneyRefund, MessageType.Added));
                Messenger.Send(new RefundViewMessage(Result.Data.MoneyRefund.Id));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при изменении оплаты заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при изменении оплаты заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving orders client");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private void SelectedCurrencyOrPaymentChanged()
        {
            if (SelectedCurrency != null)
            {
                Cashboxes = _cashboxes
                    .ForReturn(SelectedCurrency.Id, SelectedPaymentType?.Id)
                    .ForLegalEntity(parameter.LegalEntity)
                    .ToObservableCollection();

                if (Cashboxes.Count == 1)
                {
                    SelectedCashboxId = Cashboxes.First().Id;
                }
            }
            else
            {
                Cashboxes = null;
            }

            if (SelectedPaymentType?.RefundRequisitesControl == true)
            {
                Requisites = new RequisitesViewItem
                {
                    Enabled = true,
                    FirstName = _firstName,
                    LastName = _lastName,
                    MiddleName = _middleName
                };
            }
            else
            {
                Requisites = null;
            }

            RaisePropertiesChanged(nameof(SelectedCashboxId));
        }
    }
}