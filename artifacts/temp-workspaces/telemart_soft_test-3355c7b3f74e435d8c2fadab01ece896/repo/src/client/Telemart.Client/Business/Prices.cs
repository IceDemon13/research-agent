using System;

namespace Telemart.Client.Business
{
    public readonly struct Prices : IEquatable<Prices>
    {
        public Prices(decimal uah, decimal usd, decimal eur)
        {
            Uah = uah;
            Usd = usd;
            Eur = eur;
        }

        public decimal Uah { get; }

        public decimal Usd { get; }

        public decimal Eur { get; }

        public static Prices operator +(Prices a, Prices b)
        {
            return new Prices(a.Uah + b.Uah, a.Usd + b.Usd, a.Eur + b.Eur);
        }

        public static Prices operator -(Prices a, Prices b)
        {
            return new Prices(a.Uah - b.Uah, a.Usd - b.Usd, a.Eur - b.Eur);
        }

        public static bool operator ==(Prices left, Prices right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Prices left, Prices right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return CurrencyFormatingRules.ToPricesString(this);
        }

        public bool Equals(Prices other)
        {
            return Uah == other.Uah
                   && Usd == other.Usd
                   && Eur == other.Eur;
        }

        public override bool Equals(object obj)
        {
            if (obj is Prices prices)
            {
                return Equals(prices);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Uah.GetHashCode()
                   ^ Usd.GetHashCode()
                   ^ Eur.GetHashCode();
        }
    }
}