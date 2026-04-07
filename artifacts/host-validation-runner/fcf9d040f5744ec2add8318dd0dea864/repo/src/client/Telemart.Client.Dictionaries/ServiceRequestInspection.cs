namespace Telemart.Client.Dictionaries
{
    public class ServiceRequestInspection : DictionaryItemBase
    {
        public const int NotDoneId = 1;
        public const int DefectNotConfirmedId = 2;
        public const int DefectConfirmedId = 3;

        public ServiceRequestInspection(int id, string name)
            : base(id, name)
        {
        }

        public static ServiceRequestInspection NotDone { get; } = new ServiceRequestInspection(NotDoneId, "Не проводился");

        public static ServiceRequestInspection DefectNotConfirmed { get; } = new ServiceRequestInspection(DefectNotConfirmedId, "Дефект не подтвержден");

        public static ServiceRequestInspection DefectConfirmed { get; } = new ServiceRequestInspection(DefectConfirmedId, "Дефект подтвержден");
    }
}
