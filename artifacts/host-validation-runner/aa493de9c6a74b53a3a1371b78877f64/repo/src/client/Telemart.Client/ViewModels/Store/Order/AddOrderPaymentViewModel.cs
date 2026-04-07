using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddOrderPaymentViewModel : TelemartDialogViewModelBase
    {
        private List<CashboxDto> cashboxes;
        private AddOrderPaymentParameter parameter;

        public AddOrderPaymentViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public AddOrderPaymentViewModel()
        {
        }

        #region INPC

        public Currency SelectedCurrency
        {
            get { return GetProperty(() => SelectedCurrency); }
            set { SetProperty(() => SelectedCurrency, value, SelectedCurrencyOrPaymentChanged); }
        }

        public Payment SelectedPaymentType
        {
            get { return GetProperty(() => SelectedPaymentType); }
            set { SetProperty(() => SelectedPaymentType, value, SelectedCurrencyOrPaymentChanged); }
        }

        public int? SelectedCashboxId
        {
            get { return GetProperty(() => SelectedCashboxId); }
            set { SetProperty(() => SelectedCashboxId, value); }
        }

        public decimal ToPayAmount
        {
            get { return GetProperty(() => ToPayAmount); }
            set { SetProperty(() => ToPayAmount, value, () => RaisePropertyChanged(nameof(Amount))); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
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

        public DateTime ReceivedOnMinValue
        {
            get { return GetProperty(() => ReceivedOnMinValue); }
            private set { SetProperty(() => ReceivedOnMinValue, value); }
        }

        public DateTime ReceivedOnMaxValue
        {
            get { return GetProperty(() => ReceivedOnMaxValue); }
            private set { SetProperty(() => ReceivedOnMaxValue, value); }
        }

        public bool FullAmount
        {
            get { return GetProperty(() => FullAmount); }
            private set { SetProperty(() => FullAmount, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<AddOrderPaymentViewModel> builder)
        {
            builder.Property(x => x.SelectedCurrency).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedCashboxId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Amount).MatchesInstanceRule((x, y) => x > 0 && x <= y.ToPayAmount && (!y.FullAmount || x == y.ToPayAmount), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.ReceivedOn).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (AddOrderPaymentParameter)Parameter;

            ReceivedOnMinValue = parameter.MinReceivedOn;
            FullAmount = parameter.FullAmount;
            ReceivedOnMaxValue = DateTime.Today;

            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            cashboxes = cashboxes
                .Where(x => x.TypeId != CashboxType.FiscalRegistrar.Id)
                .ForLegalEntity(parameter.LegalEntity, false)
                .ToList();

            ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(parameter.ClientId));

            if (contractor.OldClient == false)
            {
                cashboxes = cashboxes.Where(x => x.LegalEntity?.White == true).ToList();
            }

            Currencies = new ObservableCollection<Currency> { Currency.Uah, Currency.Usd };

            PaymentTypes = GetPayments(parameter.PaymentId, parameter.LegalEntity?.Id).ToObservableCollection();

            Currency[] orderCurrencies = parameter.Currencies;

            if (orderCurrencies.Length == 1)
            {
                SelectedCurrency = orderCurrencies.First();
            }

            Title = "Внести оплату";
        }

        protected override Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return Task.CompletedTask;
            }

            if (ReceivedOn != DateTime.Today
                && !ShowValidationResultView("Предупреждения", new[] { new ValidationResultItem($"Дата документа ПКО будет {ReceivedOn:dd.MM.yy} 09:00, а не сегодня", false) }))
            {
                return Task.CompletedTask;
            }

            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private IEnumerable<Payment> GetPayments(int orderPaymentId, int? leagalEntityId)
        {
            if (Payment.IsCachlessPayment(orderPaymentId))
            {
                yield return Dictionaries.GetItemById<Payment>(orderPaymentId);
                yield break;
            }

            if (leagalEntityId.HasValue)
            {
                foreach (Payment payment in Dictionaries.GetItems<Payment>().Where(x => Payment.IsCachlessPayment(x.Id)))
                {
                    yield return payment;
                }
            }

            bool addWebPaymentFromClientOperationAllowed = WebClient.IsOperationAllowed(BusinessOperation.AddWebPaymentFromClient);

            foreach (Payment x in Dictionaries.GetItems<Payment>()
                .Where(x => x.Active
                    && x.Id != Payment.NoId
                    && x.Id != Payment.TerminalId
                    && (!x.OnlyCreateOnWeb || addWebPaymentFromClientOperationAllowed)
                    && !Payment.IsCachlessPayment(x.Id)
                    && (!Payment.IsCreditPayment(x.Id) || WebClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreError) || WebClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreErrorOnFirstPayment))))
            {
                yield return x;
            }
        }

        private void SelectedCurrencyOrPaymentChanged()
        {
            if (SelectedCurrency != null)
            {
                Cashboxes = cashboxes
                    .ForIncome(WebClient, SelectedCurrency.Id, SelectedPaymentType?.Id)
                    .ToObservableCollection();

                if (Cashboxes.Count == 1)
                {
                    SelectedCashboxId = Cashboxes.First().Id;
                }

                ToPayAmount = SelectedCurrency.Id == Currency.UahId
                    ? parameter.ToPayAmount.Uah
                    : parameter.ToPayAmount.Usd;
            }
            else
            {
                Cashboxes = null;
                ToPayAmount = 0m;
            }
        }
    }
}