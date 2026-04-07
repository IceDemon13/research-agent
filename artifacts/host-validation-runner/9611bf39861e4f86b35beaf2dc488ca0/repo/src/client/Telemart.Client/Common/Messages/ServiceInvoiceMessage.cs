using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceInvoiceMessage : EntityMessage<ServiceInvoiceDto>
    {
        public ServiceInvoiceMessage(ServiceInvoiceDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
