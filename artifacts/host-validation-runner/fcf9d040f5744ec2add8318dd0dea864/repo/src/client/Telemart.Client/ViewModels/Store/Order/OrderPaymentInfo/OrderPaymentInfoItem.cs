using System;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo
{
    public class OrderPaymentInfoItem : BindableBase
    {
        public OrderPaymentInfoItem(int order, string type, decimal uah, decimal usd, decimal eur)
        {
            Order = order;
            PaymentType = type;
            Value = new Prices(uah, usd, eur);
        }

        public int Order
        {
            get { return GetProperty(() => Order); }
            private set { SetProperty(() => Order, value); }
        }

        public string PaymentType
        {
            get { return GetProperty(() => PaymentType); }
            private set { SetProperty(() => PaymentType, value); }
        }

        public Prices Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => { RaisePropertiesChanged(nameof(Uah), nameof(Usd), nameof(Eur)); }); }
        }

        public decimal Uah => Value.Uah;

        public decimal Usd => Value.Usd;

        public decimal Eur => Value.Eur;

        public decimal GetByCurrency(int currencyId)
        {
            return currencyId switch
            {
                Currency.UahId => Uah,
                Currency.UsdId => Usd,
                Currency.EurId => Eur,
                _ => throw new NotSupportedException("Currency type not supported")
            };
        }

        public override string ToString()
        {
            return $"{PaymentType} {new Prices(Uah, Usd, Eur).ToString()}";
        }
    }
}