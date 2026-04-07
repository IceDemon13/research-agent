namespace Telemart.Client.Dictionaries
{
    public sealed class ProductLabel : DictionaryItem
    {
        private const int SuperPriceId = 1;
        private const int ActionId = 2;
        private const int BestSellerId = 3;
        private const int DiscountId = 4;
        private const int PriceDropId = 5;
        private const int SoonId = 6;
        private const int NewId = 7;
        private const int ExclusiveId = 8;
        private const int TradeInId = 9;
        private const int PreorderId = 10;

        private ProductLabel(int id, string name, string nameShort, string color)
            : base(id, name, true)
        {
            NameShort = nameShort;
            Color = color;
        }

        public static ProductLabel SuperPrice { get; } = new ProductLabel(SuperPriceId, "Суперцена", "Ц", "CCE6F3");

        public static ProductLabel Action { get; } = new ProductLabel(ActionId, "Акция", "А", "FFD9D9");

        public static ProductLabel BestSeller { get; } = new ProductLabel(BestSellerId, "Хит продаж", "Х", "F8F0CC");

        public static ProductLabel Discount { get; } = new ProductLabel(DiscountId, "Уценка", "У", "D8F1F5");

        public static ProductLabel PriceDrop { get; } = new ProductLabel(PriceDropId, "Распродажа", "Р", "EAD8F3");

        public static ProductLabel Soon { get; } = new ProductLabel(SoonId, "Скоро в продаже", "О", "FFE4CC");

        public static ProductLabel New { get; } = new ProductLabel(NewId, "Новинка", "Н", "DFEED4");

        public static ProductLabel Exclusive { get; } = new ProductLabel(ExclusiveId, "Эксклюзив", "Э", "DDA0DD");

        public static ProductLabel TradeIn { get; } = new ProductLabel(TradeInId, "TradeIn", "Т", "");

        public static ProductLabel Preorder { get; } = new ProductLabel(PreorderId, "Предзаказ", "П", "00ECFF00");

        public string NameShort { get; }

        public string Color { get; }

        public override string ToString()
        {
            return NameShort;
        }
    }
}