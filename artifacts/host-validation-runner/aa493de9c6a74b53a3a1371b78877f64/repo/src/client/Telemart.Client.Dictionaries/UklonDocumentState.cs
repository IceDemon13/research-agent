namespace Telemart.Client.Dictionaries
{
    public class UklonDocumentState : DictionaryItemBase
    {
        public UklonDocumentState(int id, string name, string displayName)
            : base(id, name)
        {
            DisplayName = displayName;
        }

        public static UklonDocumentState WaitingForProcessing { get; } = new UklonDocumentState(1, "В ожидании обработки", "waiting_for_processing");

        public static UklonDocumentState Processing { get; } = new UklonDocumentState(2, "В обработке", "processing");

        public static UklonDocumentState Accepted { get; } = new UklonDocumentState(3, "Принят", "accepted");

        public static UklonDocumentState Arrived { get; } = new UklonDocumentState(4, "Приехал", "arrived");

        public static UklonDocumentState Running { get; } = new UklonDocumentState(5, "В пути", "running");

        public static UklonDocumentState Returning { get; } = new UklonDocumentState(6, "Возвращается", "returning");

        public static UklonDocumentState Completed { get; } = new UklonDocumentState(7, "Завершен", "completed");

        public static UklonDocumentState Canceled { get; } = new UklonDocumentState(8, "Отменен", "canceled");

        public string DisplayName { get; }
    }
}