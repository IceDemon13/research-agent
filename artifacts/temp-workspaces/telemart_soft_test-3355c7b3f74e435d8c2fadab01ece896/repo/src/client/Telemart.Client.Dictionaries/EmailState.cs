namespace Telemart.Client.Dictionaries
{
    public class EmailState : DictionaryItem
    {
        private const int SendId = 1;

        private EmailState(int id, string name)
            : base(id, name, true)
        {
        }

        public static EmailState Send { get; } = new EmailState(SendId, "Отправлено");
    }
}