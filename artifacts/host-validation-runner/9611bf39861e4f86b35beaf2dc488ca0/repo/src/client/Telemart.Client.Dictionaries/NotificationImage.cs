namespace Telemart.Client.Dictionaries
{
    public sealed class NotificationImage : DictionaryItemBase
    {
        public const int InformationId = 1;
        public const int WarningId = 2;
        public const int ErrorId = 3;

        private NotificationImage(int id, string name)
            : base(id, name)
        {
        }

        public static NotificationImage Information { get; } = new NotificationImage(InformationId, "Информация");

        public static NotificationImage Warning { get; } = new NotificationImage(WarningId, "Предупреждение");

        public static NotificationImage Error { get; } = new NotificationImage(ErrorId, "Ошибка");
    }
}