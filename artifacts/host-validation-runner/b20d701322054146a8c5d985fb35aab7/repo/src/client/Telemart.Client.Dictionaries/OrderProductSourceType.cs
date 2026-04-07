namespace Telemart.Client.Dictionaries
{
    public class OrderProductSourceType : DictionaryItem
    {
        public const int NoneId = 0;
        public const int WarehouseSourceId = 1;
        public const int PurchaseId = 2;
        public const int MovementId = 3;
        public const int NoProductId = 4;
        public const int OtherId = 5;
        public const int LostId = 6;
        public const int GuestId = 7;

        private OrderProductSourceType(int id, string name, bool real)
            : base(id, name, true)
        {
            Real = real;
        }

        public static OrderProductSourceType None { get; } = new OrderProductSourceType(NoneId, string.Empty, false);

        public static OrderProductSourceType WarehouseSource { get; } = new OrderProductSourceType(WarehouseSourceId, "Склад", true);

        public static OrderProductSourceType Purchase { get; } = new OrderProductSourceType(PurchaseId, "Закупка", true);

        public static OrderProductSourceType Movement { get; } = new OrderProductSourceType(MovementId, "Перемещение", true);

        public static OrderProductSourceType NoProduct { get; } = new OrderProductSourceType(NoProductId, "Нет товара", false);

        public static OrderProductSourceType Other { get; } = new OrderProductSourceType(OtherId, "Прочее", false);

        public static OrderProductSourceType Lost { get; } = new OrderProductSourceType(LostId, "Утерян", false);

        public static OrderProductSourceType Guest { get; } = new OrderProductSourceType(GuestId, "Гость", false);

        public bool Real { get; }
    }
}