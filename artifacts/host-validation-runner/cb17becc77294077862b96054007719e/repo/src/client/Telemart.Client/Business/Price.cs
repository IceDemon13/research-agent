using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business
{
    public readonly struct Price
    {
        public Price(decimal value, int currencyId)
        {
            Value = value;
            CurrencyId = currencyId;
        }

        public decimal Value { get; }

        public int CurrencyId { get; }

        public static Price operator *(Price p, int k)
        {
            return new Price(p.Value * k, p.CurrencyId);
        }

        public static Prices operator +(Price p, Prices prices)
        {
            return new Prices(
                prices.Uah + (p.CurrencyId == Currency.Uah.Id ? p.Value : 0),
                prices.Usd + (p.CurrencyId == Currency.Usd.Id ? p.Value : 0),
                prices.Eur + (p.CurrencyId == Currency.Eur.Id ? p.Value : 0));
        }

        public static Prices operator +(Prices prices, Price p)
        {
            return p + prices;
        }

        public override string ToString()
        {
            return CurrencyFormatingRules.ToStr(Value, CurrencyId);
        }
    }
}