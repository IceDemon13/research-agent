namespace Telemart.Client.Dictionaries
{
    public class SendMessageType : DictionaryItem
    {
        public const int SmsId = 1;
        public const int ViberId = 2;
        public const int HybridId = 3;

        public SendMessageType(int id, string name)
            : base(id, name, true)
        {
        }

        public static SendMessageType Sms { get; } = new SendMessageType(SmsId, "SMS");

        public static SendMessageType Viber { get; } = new SendMessageType(ViberId, "Viber");
    }
}