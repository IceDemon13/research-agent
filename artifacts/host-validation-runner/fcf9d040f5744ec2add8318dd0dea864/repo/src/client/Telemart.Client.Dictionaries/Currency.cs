using System;

namespace Telemart.Client.Dictionaries
{
    public sealed class Currency : DictionaryItem
    {
        public const int UahId = 1;
        public const int UsdId = 2;
        public const int EurId = 3;

        private const string UahName = "uah";
        private const string UsdName = "usd";
        private const string EurName = "eur";

        private Currency(int id, string name, string title, int isoCodeNumber, char symbol, int decimals)
            : base(id, name, true)
        {
            Title = title;
            Symbol = symbol;
            IsoCodeNumber = isoCodeNumber;
            Decimals = decimals;
        }

        public static Currency Uah { get; } = new Currency(UahId, UahName, "UAH", 980, '₴', 0);

        public static Currency Usd { get; } = new Currency(UsdId, UsdName, "USD", 840, '$', 2);

        public static Currency Eur { get; } = new Currency(EurId, EurName, "EUR", 978, '€', 2);

        public string Title { get; }

        public int IsoCodeNumber { get; }

        public int Decimals { get; }

        public char Symbol { get; }

        public static Currency GetById(int currencyId)
        {
            Currency currencyType;

            switch (currencyId)
            {
                case UahId:
                    currencyType = Uah;
                    break;
                case UsdId:
                    currencyType = Usd;
                    break;
                case EurId:
                    currencyType = Eur;
                    break;
                default:
                    throw new NotSupportedException($"Currency id:{currencyId} not supported.");
            }

            return currencyType;
        }

        public static Currency GetByName(string currencyName)
        {
            Currency currencyType;

            switch (currencyName)
            {
                case UahName:
                    currencyType = Uah;
                    break;
                case UsdName:
                    currencyType = Usd;
                    break;
                case EurName:
                    currencyType = Eur;
                    break;
                default:
                    throw new NotSupportedException($"Currency name:{currencyName} not supported.");
            }

            return currencyType;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}