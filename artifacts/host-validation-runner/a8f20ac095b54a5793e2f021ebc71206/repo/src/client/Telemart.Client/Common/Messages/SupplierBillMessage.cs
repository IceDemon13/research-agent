using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Common.Messages
{
    public class SupplierBillMessage : EntityMessage<SupplierBillDto>
    {
        public SupplierBillMessage(SupplierBillDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
