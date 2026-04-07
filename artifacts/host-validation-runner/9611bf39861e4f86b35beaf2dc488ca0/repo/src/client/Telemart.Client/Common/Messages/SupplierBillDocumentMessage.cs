using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Common.Messages
{
    public class SupplierBillDocumentMessage : EntityMessage<SupplierBillDocumentDto>
    {
        public SupplierBillDocumentMessage(SupplierBillDocumentDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
