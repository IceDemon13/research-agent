namespace Telemart.Client.Common.Messages
{
    public sealed class UpdateUserNotificationsMessage
    {
        public UpdateUserNotificationsMessage(int messageCount, bool important)
        {
            MessageCount = messageCount;
            Important = important;
        }

        public int MessageCount { get; }

        public bool Important { get; }
    }
}
