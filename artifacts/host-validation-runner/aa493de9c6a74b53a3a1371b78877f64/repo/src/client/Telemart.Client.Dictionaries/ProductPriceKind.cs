using System.Globalization;

namespace Telemart.Client.Dictionaries
{
    public sealed class ProductPriceKind : DictionaryItem
    {
        public const int Telemart1 = 1;
        public const int Telemart2 = 6;
        public const int Telemart3 = 9;
        public const int Telemart4 = 10;
        public const int Telemart5= 11;
        public const int Configurator1 = 12;
        public const int Configurator2 = 13;
        public const int Configurator3 = 14;
        public const int Configurator4 = 15;
        public const int Configurator5 = 16;
        public const int Configurator6 = 17;
        public const int Configurator7 = 18;
        public const int Configurator8 = 19;
        public const int Configurator9 = 20;
        public const int Configurator10 = 21;

        public ProductPriceKind(int id, string name, bool configurator, int? priceColumn, int currencyId, bool canSwitchInOrders)
            : base(id, name, true)
        {
            Real = !configurator;
            Configurator = configurator;
            PriceColumn = priceColumn;
            CurrencyId = currencyId;
            CanSwitchInOrders = canSwitchInOrders;
        }

        public bool Real { get; }

        public bool Configurator { get; }

        public int? PriceColumn { get; }

        public bool CanSwitchInOrders { get; }

        public int CurrencyId { get; }
    }
}