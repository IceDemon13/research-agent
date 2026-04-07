namespace Telemart.Client.Dictionaries
{
    public sealed class Department : DictionaryItem
    {
        private const int CallCenterId = 1;
        private const int PurchaseId = 2;
        private const int PickupId = 3;
        private const int WarehouseId = 4;
        private const int LogisticId = 5;
        private const int ServiceId = 6;
        private const int MarketingId = 7;
        private const int ContentId = 8;
        private const int ItId = 9;
        private const int BookkeepingId = 10;
        private const int HrId = 11;
        private const int CompTechId = 12;
        private const int OtherId = 13;

        private Department(int id, string name)
            : base(id, name, true)
        {
        }

        public static Department CallCenter { get; } = new Department(CallCenterId, "КЦ");

        public static Department Purchase { get; } = new Department(PurchaseId, "Закупка");

        public static Department Pickup { get; } = new Department(PickupId, "Самовывоз");

        public static Department Warehouse { get; } = new Department(WarehouseId, "Склад");

        public static Department Logistic { get; } = new Department(LogisticId, "Логистика");

        public static Department Service { get; } = new Department(ServiceId, "Сервис");

        public static Department Marketing { get; } = new Department(MarketingId, "Маркетинг");

        public static Department Content { get; } = new Department(ContentId, "Контент");

        public static Department It { get; } = new Department(ItId, "IT");

        public static Department Bookkeeping { get; } = new Department(BookkeepingId, "Бухгалтерия");

        public static Department Hr { get; } = new Department(HrId, "HR");

        public static Department CompTech { get; } = new Department(CompTechId, "Комп. техника");

        public static Department Other { get; } = new Department(OtherId, "Прочее");
    }
}
