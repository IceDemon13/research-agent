using System;
using System.Diagnostics.CodeAnalysis;

namespace Telemart.Client.Business
{
    public struct OrderPrices : IEquatable<OrderPrices>
    {
        public OrderPrices(Prices total, Prices payed, Prices delivery)
        {
            Total = total;
            Payed = payed;
            Delivery = delivery;
            ToPay = Total + Delivery - Payed;
        }

        public Prices Total { get; }

        public Prices Payed { get; }

        public Prices ToPay { get; }

        public Prices Delivery { get; }

        public static bool operator ==(OrderPrices left, OrderPrices right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OrderPrices left, OrderPrices right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is OrderPrices orderPrices)
            {
                return Equals(orderPrices);
            }

            return false;
        }

        public bool Equals([AllowNull] OrderPrices other)
        {
            return Total == other.Total
                && Payed == other.Payed
                && Delivery == other.Delivery;
        }

        public override int GetHashCode()
        {
            return Total.GetHashCode()
                ^ Payed.GetHashCode()
                ^ Delivery.GetHashCode();
        }
    }
}