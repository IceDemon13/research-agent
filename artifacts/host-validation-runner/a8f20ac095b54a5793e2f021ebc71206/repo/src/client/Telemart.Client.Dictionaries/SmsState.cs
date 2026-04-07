namespace Telemart.Client.Dictionaries
{
    public class SmsState : DictionaryItem
    {
        private const int WaitsForSendingId = 1;
        private const int SentId = 2;
        private const int ErrorId = 3;
        private const int CreatedId = 4;

        private SmsState(int id, string name)
            : base(id, name, true)
        {
        }

        public static SmsState WaitsForSending { get; } = new SmsState(WaitsForSendingId, "Ожидает отправки");

        public static SmsState Sent { get; } = new SmsState(SentId, "Отправлено");

        public static SmsState Error { get; } = new SmsState(ErrorId, "Ошибка");

        public static SmsState Created { get; } = new SmsState(CreatedId, "Создано");
    }
}