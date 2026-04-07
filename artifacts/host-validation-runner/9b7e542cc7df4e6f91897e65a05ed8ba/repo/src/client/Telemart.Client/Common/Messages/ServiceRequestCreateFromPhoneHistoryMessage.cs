namespace Telemart.Client.Common.Messages
{
    public class ServiceRequestCreateFromPhoneHistoryMessage
    {
        public ServiceRequestCreateFromPhoneHistoryMessage(int orderId)
        {
            OrderId = orderId;
        }

        public int OrderId { get; }
    }
}