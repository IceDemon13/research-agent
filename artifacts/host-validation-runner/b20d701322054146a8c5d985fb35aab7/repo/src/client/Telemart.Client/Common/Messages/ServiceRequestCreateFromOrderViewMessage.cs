namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRequestCreateFromOrderViewMessage
    {
        public ServiceRequestCreateFromOrderViewMessage(int orderId, int productId)
        {
            OrderId = orderId;
            ProductId = productId;
        }

        public int OrderId { get; }

        public int ProductId { get; }
    }
}