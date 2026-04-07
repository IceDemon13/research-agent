namespace Telemart.Client.Dictionaries
{
    public sealed class TradeInIndicator : DictionaryItemBase
    {
        public const int NoWarrantyId = 5;
        public const int LessThan1YearId = 6;
        public const int MoreThan1YearId = 7;

        private const int WarrantyId = 1;
        private const int ClassId = 2;
        private const int PackageId = 3;

        private TradeInIndicator(int id, string name)
            : base(id, name)
        {
        }

        public static TradeInIndicator Warranty { get; } = new TradeInIndicator(WarrantyId, "Гарантия");

        public static TradeInIndicator Class { get; } = new TradeInIndicator(ClassId, "Класс");

        public static TradeInIndicator Package { get; } = new TradeInIndicator(PackageId, "Упаковка");
    }
}