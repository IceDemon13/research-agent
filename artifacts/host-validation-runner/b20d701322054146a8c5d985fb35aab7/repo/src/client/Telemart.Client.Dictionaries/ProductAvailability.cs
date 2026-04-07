namespace Telemart.Client.Dictionaries
{
    public sealed class ProductAvailability : DictionaryItem
    {
        private const int ArchiveId = 1;
        private const int NotAvailId = 2;
        private const int ClarifyId = 3;
        private const int ComingSoonId = 4;
        private const int AvailId = 5;
        private const int InReserveId = 6;
        private const int InStockId = 7;
        private const int EndsId = 8;

        private ProductAvailability(int id, string name, string nameShort, ProductAvailabilityType type, bool canBuy, bool active, int weight)
            : base(id, name, active)
        {
            NameShort = nameShort;
            Type = type;
            CanBuy = canBuy;
            Weight = weight;
        }

        public static ProductAvailability Archive { get; } = new ProductAvailability(ArchiveId, "Архивный", "С", ProductAvailabilityType.NotInStock, false, true, 10);

        public static ProductAvailability NotAvail { get; } = new ProductAvailability(NotAvailId, "Нет в наличии", "-", ProductAvailabilityType.NotInStock, false, true, 20);

        public static ProductAvailability Clarify { get; } = new ProductAvailability(ClarifyId, "Уточняйте", "?", ProductAvailabilityType.Unknown, true, true, 30);

        public static ProductAvailability ComingSoon { get; } = new ProductAvailability(ComingSoonId, "Ожидается", "О", ProductAvailabilityType.Expected, false, true, 40);

        public static ProductAvailability Avail { get; } = new ProductAvailability(AvailId, "Есть в наличии", "+", ProductAvailabilityType.InStock, true, true, 50);

        public static ProductAvailability InReserve { get; } = new ProductAvailability(InReserveId, "В резерве", "Б", ProductAvailabilityType.Unknown, false, false, 31);

        public static ProductAvailability InStock { get; } = new ProductAvailability(InStockId, "На складе", "Н", ProductAvailabilityType.InStock, false, false, 55);

        public static ProductAvailability Ends { get; } = new ProductAvailability(EndsId, "Заканчивается", "З", ProductAvailabilityType.InStock, false, false, 48);

        public string NameShort { get; }

        public ProductAvailabilityType Type { get; }

        public bool CanBuy { get; }

        public int Weight { get; }

        public override string ToString()
        {
            return $"{Name} ({NameShort})";
        }
    }
}
