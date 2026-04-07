namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestResolution : DictionaryItem
    {
        public const int NoId = 1;
        public const int ConfirmedId = 2;
        public const int RejectedId = 3;
        public const int DoneId = 4;
        public const int RemovedFromRegisterId = 5;

        private ServiceRequestResolution(int id, string name, string value)
            : base(id, name, true)
        {
            Value = value;
        }

        public static ServiceRequestResolution No { get; } = new ServiceRequestResolution(NoId, "Нет", "No");

        public static ServiceRequestResolution Confirmed { get; } = new ServiceRequestResolution(ConfirmedId, "Подтверждено", "Confirmed");

        public static ServiceRequestResolution Rejected { get; } = new ServiceRequestResolution(RejectedId, "Отклонено", "Rejected");

        public static ServiceRequestResolution Done { get; } = new ServiceRequestResolution(DoneId, "Выполнено", "Done");

        public static ServiceRequestResolution RemovedFromRegister { get; } = new ServiceRequestResolution(RemovedFromRegisterId, "Товар списан", "RemovedFromRegister");

        public string Value { get; }
    }
}