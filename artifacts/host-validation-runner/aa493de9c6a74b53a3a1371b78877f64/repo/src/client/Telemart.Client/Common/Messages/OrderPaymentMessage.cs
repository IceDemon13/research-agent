using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrderPaymentMessage : EntityMessage<OrderPaymentDto>
    {
        public OrderPaymentMessage(OrderPaymentDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}