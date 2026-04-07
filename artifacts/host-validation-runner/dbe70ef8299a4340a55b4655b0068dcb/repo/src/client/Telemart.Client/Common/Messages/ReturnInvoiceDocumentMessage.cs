using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Common.Messages
{
    public class ReturnInvoiceDocumentMessage : EntityMessage<ReturnInvoiceDocumentSimpleDto>
    {
        public ReturnInvoiceDocumentMessage(ReturnInvoiceDocumentSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
