namespace Telemart.Client.Dictionaries
{
    public class InvoiceAdditionalCostType : DictionaryItem
    {
        public const int AmountId = 1;
        public const int QuantityId = 2;
        public const int WeightId = 3;

        private InvoiceAdditionalCostType(int id, string name)
            : base(id, name, true)
        {
        }

        public static InvoiceAdditionalCostType Amount { get; } = new InvoiceAdditionalCostType(AmountId, "Сумма");

        public static InvoiceAdditionalCostType Quantity { get; } = new InvoiceAdditionalCostType(QuantityId, "Количество");

        public static InvoiceAdditionalCostType Weight { get; } = new InvoiceAdditionalCostType(WeightId, "Вес");

    }
}
