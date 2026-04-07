namespace Telemart.Client.Dictionaries
{
    public sealed class TaskState : DictionaryItem
    {
        private const int NewId = 1;
        private const int InProgressId = 2;
        private const int CompletedId = 3;
        private const int CancelledId = 4;

        private TaskState(int id, string name)
            : base(id, name, true)
        {
        }

        public static TaskState New { get; } = new TaskState(NewId, "Новая");

        public static TaskState InProgress { get; } = new TaskState(InProgressId, "В работе");

        public static TaskState Completed { get; } = new TaskState(CompletedId, "Выполнена");

        public static TaskState Cancelled { get; } = new TaskState(CancelledId, "Отменена");
    }
}
