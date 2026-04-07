namespace Telemart.Client.Dictionaries
{
    public class SupplierBillState : DictionaryItem
    {
        private const int NewId = 1;
        private const int ProcessedId = 2;
        private const int CompletedId = 3;
        private const int CanceledId = 4;

        private SupplierBillState(int id, string name)
            : base(id, name, true)
        {
        }

        public static SupplierBillState New { get; } = new SupplierBillState(NewId, "Новый");

        public static SupplierBillState Processed { get; } = new SupplierBillState(ProcessedId, "Обработан");

        public static SupplierBillState Completed { get; } = new SupplierBillState(CompletedId, "Выполнен");

        public static SupplierBillState Canceled { get; } = new SupplierBillState(CanceledId, "Отменен");
    }
}
