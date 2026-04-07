namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestState : DictionaryItem
    {
        private const int NewId = 1;
        private const int InProgressId = 2;
        private const int ReadyId = 3;
        private const int InRepairId = 4;
        private const int AcceptedId = 5;
        private const int CompletedId = 6;
        private const int CancelledId = 7;
        private const int OnConfirmationId = 8;

        private ServiceRequestState(int id, string name, int position, bool inProgress = false, bool completed = false)
            : base(id, name, true)
        {
            Position = position;
            InProgressFlag = inProgress;
            CompletedFlag = completed;
        }

        public static ServiceRequestState New { get; } = new ServiceRequestState(NewId, "Обращение", 1);

        public static ServiceRequestState InProgress { get; } = new ServiceRequestState(InProgressId, "В работе", 3, true);

        public static ServiceRequestState Ready { get; } = new ServiceRequestState(ReadyId, "Готова", 5);

        public static ServiceRequestState InRepair { get; } = new ServiceRequestState(InRepairId, "В СЦ", 4, true);

        public static ServiceRequestState Accepted { get; } = new ServiceRequestState(AcceptedId, "Принята", 2, true);

        public static ServiceRequestState Completed { get; } = new ServiceRequestState(CompletedId, "Завершена", 6, completed: true);

        public static ServiceRequestState Cancelled { get; } = new ServiceRequestState(CancelledId, "Отменена", 7, completed: true);

        public static ServiceRequestState OnConfirmation { get; } = new ServiceRequestState(OnConfirmationId, "На согласовании", 8, true);

        public int Position { get; }

        public bool InProgressFlag { get; }

        public bool CompletedFlag { get; }
    }
}