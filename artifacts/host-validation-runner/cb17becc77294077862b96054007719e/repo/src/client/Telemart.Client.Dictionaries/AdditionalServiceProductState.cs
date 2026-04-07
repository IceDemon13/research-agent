namespace Telemart.Client.Dictionaries
{
    public sealed class AdditionalServiceProductState : DictionaryItem
    {
        public const int WaitingId = 1;
        public const int WarehouseId = 2;
        public const int CompletedId = 3;
        public const int DoingId = 4;

        public AdditionalServiceProductState(int id, string name)
            : base(id, name, true)
        {
        }

        public static AdditionalServiceProductState Waiting { get; } = new AdditionalServiceProductState(WaitingId, "Ожидается");

        public static AdditionalServiceProductState Warehouse { get; } = new AdditionalServiceProductState(WarehouseId, "На складе");

        public static AdditionalServiceProductState Completed { get; } = new AdditionalServiceProductState(CompletedId, "Завершена");

        public static AdditionalServiceProductState Doing { get; } = new AdditionalServiceProductState(DoingId, "Выполняется");
    }
}