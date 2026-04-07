using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order.OrderDocuments
{
    public sealed class OrderCreateDocumentMessage : EntityMessage<OrderDocumentDto>
    {
        public OrderCreateDocumentMessage(OrderDocumentDto itemDto, MessageType messageType, OrderDto order)
            : base(itemDto, messageType)
        {
            Order = order;
        }

        public OrderDto Order { get; }
    }
}