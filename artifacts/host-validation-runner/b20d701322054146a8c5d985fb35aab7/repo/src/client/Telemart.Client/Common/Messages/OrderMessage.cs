using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrderMessage : EntityMessage<OrderDto>
    {
        public OrderMessage(OrderDto order, MessageType messageType)
            : base(order, messageType)
        {
        }
    }
}