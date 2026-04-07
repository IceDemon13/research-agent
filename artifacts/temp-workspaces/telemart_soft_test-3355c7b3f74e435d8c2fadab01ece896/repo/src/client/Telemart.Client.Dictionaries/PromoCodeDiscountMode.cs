namespace Telemart.Client.Dictionaries
{
    public class PromoCodeDiscountMode : DictionaryItem
    {
        private const int PercentId = 1;
        private const int PriceDiscountId = 2;
        private const int PriceId = 3;
        public const int CashbackPercentageId = 4;
        public const int FixedCashbackId = 5;

        public PromoCodeDiscountMode(int id, string name)
            : base(id, name, true)
        {
        }

        public static PromoCodeDiscountMode Percent { get; } = new PromoCodeDiscountMode(PercentId, "Скидка в %");

        public static PromoCodeDiscountMode PriceDiscount { get; } = new PromoCodeDiscountMode(PriceDiscountId, "Скидка в грн");

        public static PromoCodeDiscountMode Price { get; } = new PromoCodeDiscountMode(PriceId, "Конкретная цена");

        public static PromoCodeDiscountMode CashbackPercentage { get; } = new PromoCodeDiscountMode(CashbackPercentageId, "Кешбек в %");

        public static PromoCodeDiscountMode FixedCashback { get; } = new PromoCodeDiscountMode(FixedCashbackId, "Фиксированный кешбек");
    }
}
