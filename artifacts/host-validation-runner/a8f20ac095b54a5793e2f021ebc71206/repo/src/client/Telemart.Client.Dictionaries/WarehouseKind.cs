namespace Telemart.Client.Dictionaries
{
    public sealed class WarehouseKind : DictionaryItem
    {
        public const int MainId = 1;
        public const int ServiceId = 2;
        public const int PickupId = 3;
        public const int VirtualId = 5;
        public const int FixedAssestsId = 6;
        public const int ShowCaseId = 7;
        public const int AssemblyId = 8;
        public const int IssuingWarehouseId = 9;
        public const int AssemblyWarehouseId = 10;
        public const int ServiceWarehouseId = 11;
        public const int BufferWarehouseId = 12;
        public const int ReserveWarehouseForConfigurationId = 13;

        private WarehouseKind(int id, string name, bool active, bool isVirtual = false)
            : base(id, name, active)
        {
            IsVirtual = isVirtual;
        }
        
        public bool IsVirtual { get; }

        public static WarehouseKind Main { get; } = new WarehouseKind(MainId, "Основной", true);

        public static WarehouseKind Service { get; } = new WarehouseKind(ServiceId, "Сервисный", true);

        public static WarehouseKind Pickup { get; } = new WarehouseKind(PickupId, "Самовывоз", true);

        public static WarehouseKind Virtual { get; } = new WarehouseKind(VirtualId, "Виртуальный", true);

        public static WarehouseKind FixedAssests { get; } = new WarehouseKind(FixedAssestsId, "Основные средства", true);

        public static WarehouseKind ShowCase { get; } = new WarehouseKind(ShowCaseId, "Витрина", true);

        public static WarehouseKind Assembly { get; } = new WarehouseKind(AssemblyId, "Сборка", true);
        
        public static WarehouseKind IssuingWarehouse { get; } = new WarehouseKind(IssuingWarehouseId, "Склад выдачи", true, true);

        public static WarehouseKind AssemblyWarehouse { get; } = new WarehouseKind(AssemblyWarehouseId, "Склад сборки", true, true);

        public static WarehouseKind ServiceWarehouse { get; } = new WarehouseKind(ServiceWarehouseId, "Склад оказания услуг", true, true);

        public static WarehouseKind BufferWarehouse { get; } = new WarehouseKind(BufferWarehouseId, "Буферный склад", true, true);

        public static WarehouseKind ReserveWarehouseForConfiguration { get; } = new WarehouseKind(ReserveWarehouseForConfigurationId, "Склад резерва под конфигурации", true, true);
    }
}