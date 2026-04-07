namespace Telemart.Client.Common.Messages
{
    public class OrderEditReopenViewMessage
    {
        public OrderEditReopenViewMessage(int orderId, bool isAdd = false)
        {
            OrderId = orderId;
            IsAdd = isAdd;
        }

        public int OrderId { get; }

        public bool IsAdd { get; }
    }
}