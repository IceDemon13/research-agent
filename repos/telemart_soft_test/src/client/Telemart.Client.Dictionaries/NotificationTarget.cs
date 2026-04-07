namespace Telemart.Client.Dictionaries
{
    public sealed class NotificationTarget : DictionaryItemBase
    {
        private const int TelemartClientId = 1;
        private const int TelegramId = 2;

        private NotificationTarget(int id, string name)
            : base(id, name)
        {
        }

        public static NotificationTarget TelemartClient { get; } = new NotificationTarget(TelemartClientId, "Telemart.Client");

        public static NotificationTarget Telegram { get; } = new NotificationTarget(TelegramId, "Telegram");
    }
}