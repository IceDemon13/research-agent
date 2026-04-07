namespace Telemart.Client.Dictionaries
{
    public class ServiceCenterType : DictionaryItem
    {
        public const int ServiceCenterId = 1;
        public const int SupplierId = 2;

        public ServiceCenterType(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static ServiceCenterType ServiceCenter { get; } = new ServiceCenterType(ServiceCenterId, "СЦ");

        public static ServiceCenterType Supplier { get; } = new ServiceCenterType(SupplierId, "Поставщик");
    }
}
