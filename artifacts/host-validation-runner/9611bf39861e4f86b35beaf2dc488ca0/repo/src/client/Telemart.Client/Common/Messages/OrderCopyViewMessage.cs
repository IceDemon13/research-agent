using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrderCopyViewMessage
    {
        public OrderCopyViewMessage(OrderDto copiedOrder)
        {
            CopiedOrder = copiedOrder;
        }

        public OrderDto CopiedOrder { get; }
    }
}
