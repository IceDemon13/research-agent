namespace Telemart.Client.Dictionaries
{
    public class NovaposhtaTtnServiceType : DictionaryItem
    {
        public const int DoorsDoorsId = 1;
        public const int WarehouseWarehouseId = 2;
        public const int WarehouseDoorsId = 3;
        public const int DoorsWarehouseId = 4;

        private NovaposhtaTtnServiceType(int id, string name)
            : base(id, name, true)
        {
        }

        public static NovaposhtaTtnServiceType DoorsDoors { get; } = new NovaposhtaTtnServiceType(DoorsDoorsId, "Адрес-Адрес");

        public static NovaposhtaTtnServiceType WarehouseWarehouse { get; } = new NovaposhtaTtnServiceType(WarehouseWarehouseId, "Склад-Склад");

        public static NovaposhtaTtnServiceType WarehouseDoors { get; } = new NovaposhtaTtnServiceType(WarehouseDoorsId, "Склад-Адрес");

        public static NovaposhtaTtnServiceType DoorsWarehouse { get; } = new NovaposhtaTtnServiceType(DoorsWarehouseId, "Адрес-Склад");
    }
}
