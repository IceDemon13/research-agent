using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrderBillMessage : EntityMessage<OrderBillDto>
    {
        public OrderBillMessage(OrderBillDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
