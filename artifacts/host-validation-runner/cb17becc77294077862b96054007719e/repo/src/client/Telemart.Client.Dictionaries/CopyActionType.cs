namespace Telemart.Client.Dictionaries
{
    public class CopyActionType : DictionaryItem
    {
        private const int OverrideId = 1;
        private const int AddId = 2;

        public CopyActionType(int id, string name)
            : base(id, name, true)
        {
        }

        public static CopyActionType Override { get; } = new CopyActionType(OverrideId, "Переопределить");

        public static CopyActionType Add { get; } = new CopyActionType(AddId, "Добавить");
    }
}