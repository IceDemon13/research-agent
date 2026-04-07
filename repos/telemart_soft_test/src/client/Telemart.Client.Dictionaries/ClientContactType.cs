namespace Telemart.Client.Dictionaries
{
    public sealed class ClientContactType : DictionaryItem
    {
        public const int CallId = 1;
        public const int SmsId = 2;
        public const int EmailId = 3;
        public const int ViberId = 4;

        private ClientContactType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ClientContactType Call { get; } = new ClientContactType(CallId, "Звонок");

        public static ClientContactType Sms { get; } = new ClientContactType(SmsId, "SMS");

        public static ClientContactType Email { get; } = new ClientContactType(EmailId, "E-Mail");

        public static ClientContactType Viber { get; } = new ClientContactType(ViberId, "Viber");

    }
}