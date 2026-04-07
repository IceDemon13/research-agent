namespace Telemart.Client.Dictionaries
{
    public class ServiceCenterRepairConfirmType : DictionaryItem
    {
        public const int NotRequiredId = 1;
        public const int RequiredId = 2;

        private ServiceCenterRepairConfirmType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceCenterRepairConfirmType NotRequired { get; } = new ServiceCenterRepairConfirmType(NotRequiredId, "Не требуется");

        public static ServiceCenterRepairConfirmType Required { get; } = new ServiceCenterRepairConfirmType(RequiredId, "Требуется");
    }
}
