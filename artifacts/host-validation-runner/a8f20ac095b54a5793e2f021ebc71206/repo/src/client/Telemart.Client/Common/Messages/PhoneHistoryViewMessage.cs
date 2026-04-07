namespace Telemart.Client.Common.Messages
{
    public class PhoneHistoryViewMessage
    {
        public PhoneHistoryViewMessage(params string[] phones)
        {
            Phones = phones;
        }

        public string[] Phones { get; }
    }
}
