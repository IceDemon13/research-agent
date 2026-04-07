namespace Telemart.Client.Dictionaries
{
    public class OrderBillState : DictionaryItem
    {
        private const int NewId = 1;
        private const int PaidId = 2;
        private const int ExpiredId = 3;

        private OrderBillState(int id, string name)
            : base(id, name, true)
        {
        }

        public static OrderBillState New { get; } = new OrderBillState(NewId, "Новый");

        public static OrderBillState Paid { get; } = new OrderBillState(PaidId, "Оплачен");

        public static OrderBillState Expired { get; } = new OrderBillState(ExpiredId, "Не актуален");
    }
}
