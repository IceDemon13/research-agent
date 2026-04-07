using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class InvoiceMessage : EntityMessage<InvoiceDto>
    {
        public InvoiceMessage(InvoiceDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
