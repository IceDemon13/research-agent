using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Common.Messages
{
    public sealed class ReturnInvoiceMessage : EntityMessage<ReturnInvoiceDto>
    {
        public ReturnInvoiceMessage(ReturnInvoiceDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
