namespace Telemart.Client.Dictionaries
{
    public class BacklogTaskResolution : DictionaryItem
    {
        public const int CompletedId = 1;
        public const int CanceledId = 2;
        public const int DublicteId = 3;

        private BacklogTaskResolution(int id, string name)
            : base(id, name, true)
        {
        }

        public static BacklogTaskResolution Completed { get; } = new BacklogTaskResolution(CompletedId, "Выполнена");

        public static BacklogTaskResolution Canceled { get; } = new BacklogTaskResolution(CanceledId, "Отклонена");

        public static BacklogTaskResolution Dublicate { get; } = new BacklogTaskResolution(DublicteId, "Дубль");
    }
}
