namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRepairType : DictionaryItem
    {
        private const int WarrantyId = 1;
        private const int PaidId = 2;

        public ServiceRepairType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRepairType Warranty { get; } = new ServiceRepairType(WarrantyId, "Гарантийный");

        public static ServiceRepairType Paid { get; } = new ServiceRepairType(PaidId, "Платный");
    }
}