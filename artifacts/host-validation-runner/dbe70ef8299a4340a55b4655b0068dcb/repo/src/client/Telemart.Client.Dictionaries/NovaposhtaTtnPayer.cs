namespace Telemart.Client.Dictionaries
{
    public sealed class NovaposhtaTtnPayer : DictionaryItem
    {
        private const int SenderId = 1;
        private const int RecipientId = 2;
        private const int ThirdPersonId = 3;

        public NovaposhtaTtnPayer(int id, string name)
            : base(id, name, true)
        {
        }

        public static NovaposhtaTtnPayer Sender { get; } = new NovaposhtaTtnPayer(SenderId, "Отправитель");

        public static NovaposhtaTtnPayer Recipient { get; } = new NovaposhtaTtnPayer(RecipientId, "Получатель");

        public static NovaposhtaTtnPayer ThirdPerson { get; } = new NovaposhtaTtnPayer(ThirdPersonId, "Третье лицо");
    }
}