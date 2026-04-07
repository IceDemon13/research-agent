namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestLocation : DictionaryItem
    {
        public const int ClientId = 1;
        public const int WarehouseId = 2;
        public const int ServiceId = 3;
        public const int SupplierId = 4;
        public const int OnTheWayId = 5;

        private ServiceRequestLocation(int id, string name, string value)
            : base(id, name, true)
        {
            Value = value;
        }

        public static ServiceRequestLocation Client { get; } = new ServiceRequestLocation(ClientId, "Клиент", "Client");

        public static ServiceRequestLocation Warehouse { get; } = new ServiceRequestLocation(WarehouseId, "Склад", "Warehouse");

        public static ServiceRequestLocation Service { get; } = new ServiceRequestLocation(ServiceId, "Сервис", "Service");

        public static ServiceRequestLocation Supplier { get; } = new ServiceRequestLocation(SupplierId, "Поставщик", "Supplier");

        public static ServiceRequestLocation OnTheWay { get; } = new ServiceRequestLocation(OnTheWayId, "В пути", "OnTheWay");

        public string Value { get; }
    }
}