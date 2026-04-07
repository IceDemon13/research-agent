using System.Linq;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Common.Dictionaries;

namespace Telemart.Client.Extensions
{
    public static class OrderExtentions
    {
        public static OrderPrices GetPrices(this OrderDto order)
        {
            return new OrderPrices(GetTotalAmount(order), GetPayedAmount(order), GetDeliveryCostAmount(order));
        }

        public static Prices GetPrepayment(this OrderDto order)
        {
            Prices prepayment = new Prices(0, 0, 0);

            foreach (Price payment in order.OrderPayments.Select(x => new Price(x.Sign * x.Amount, x.CurrencyId)))
            {
                prepayment += payment;
            }

            return prepayment;
        }

        public static Prices GetDeliveryCostAmount(this OrderDto order)
        {
            return new Prices(order.PackageDeliveryCost, 0, 0);
        }

        public static Prices GetDeliveryCostAmount(this OrderSimpleDto order)
        {
            return new Prices(order.PackageDeliveryCost, 0, 0);
        }

        public static Prices GetPayedAmount(this OrderDto order)
        {
            Prices prices = new Prices(0, 0, 0);

            foreach (Price price in order.OrderPayments.Select(p => new Price(p.Sign * p.Amount, p.CurrencyId)))
            {
                prices += price;
            }

            return prices;
        }

        public static Prices GetTotalAmount(this OrderDto order)
        {
            Prices prices = new Prices(0, 0, 0);

            foreach (Price price in order.Products.Select(x => new Price(x.PriceOut * x.Quantity, x.CurrencyOutId)))
            {
                prices += price;
            }

            return prices;
        }

        public static Prices GetLeftToPay(this OrderDto order)
        {
            return GetTotalAmount(order) + GetDeliveryCostAmount(order) - GetPayedAmount(order);
        }

        public static Prices GetTotalAmount(this OrderSimpleDto order)
        {
            Prices prices = new Prices(0, 0, 0);

            foreach (Price price in order.Products.Select(x => new Price(x.PriceOut * x.Quantity, x.CurrencyOutId)))
            {
                prices += price;
            }

            return prices;
        }

        public static bool IsPartialRefund(this OrderDto order)
        {
            return order.OrderPayments?.Any(x => (Payment.IsCreditPayment(x.PaymentId) || x.PaymentId == PaymentIds.TerminalId || x.PaymentId == PaymentIds.BonusesId) && x.Sign > 0) == true;
        }
    }
}