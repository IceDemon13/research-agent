namespace Telemart.Client.Dictionaries
{
     public sealed class ServiceProductType : DictionaryItem
    {
        private const int DiscountId = 1;
        private const int TradeInId = 2;

        private ServiceProductType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceProductType Discount { get; } = new ServiceProductType(DiscountId, "Уценка");

        public static ServiceProductType TradeIn { get; } = new ServiceProductType(TradeInId, "Trade-In");
    }
}