namespace Telemart.Client.Dictionaries
{
    public class WorkPlaceDeviceType : DictionaryItem
    {
        private const int PcId = 1;
        private const int LaptopId = 2;

        public WorkPlaceDeviceType(int id, string name)
            : base(id, name, true)
        {
        }

        public static WorkPlaceDeviceType Pc { get; } = new WorkPlaceDeviceType(PcId, "ПК");

        public static WorkPlaceDeviceType Laptop { get; } = new WorkPlaceDeviceType(LaptopId, "Ноутбук");
    }
}