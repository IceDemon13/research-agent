namespace Telemart.Client.Dictionaries
{
    public sealed class PromoCodeType : DictionaryItemBase
    {
        private const int ProductDiscountId = 1;
        private const int BundleId = 2;

        private PromoCodeType(int id, string name)
            : base(id, name)
        {
        }

        public static PromoCodeType ProductDiscount { get; } = new PromoCodeType(ProductDiscountId, "Скидка на товар");

        public static PromoCodeType Bundle { get; } = new PromoCodeType(BundleId, "Бандл");
    }
}