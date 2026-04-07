using DevExpress.Mvvm;
using Telemart.Client.Business;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    public class BankPaymentConfirmViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? ParsedOrderId
        {
            get { return GetProperty(() => ParsedOrderId); }
            set { SetProperty(() => ParsedOrderId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value, () => RaisePropertyChanged(nameof(AmountFormatted))); }
        }

        public string AmountFormatted => CurrencyFormatingRules.ToStr(Amount, CurrencyId, "C2");

        public string SumToPayFormatted => Order != null
            ? CurrencyFormatingRules.ToCurrencyPriceString(Order.Prices.ToPay, CurrencyId, "C2")
            : string.Empty;

        public string PayedSumFormatted => Order != null
            ? CurrencyFormatingRules.ToCurrencyPriceString(Order.Prices.Payed, CurrencyId, "C2")
            : string.Empty;

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value, () => RaisePropertyChanged(nameof(AmountFormatted))); }
        }

        public BankPaymentConfirmOrderViewItem Order
        {
            get { return GetProperty(() => Order); }
            set { SetProperty(() => Order, value, () => RaisePropertiesChanged(nameof(SumToPayFormatted), nameof(PayedSumFormatted))); }
        }
    }
}
