using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Common.Messages
{
    public class ServiceProductMessage : EntityMessage<ServiceProductDto>
    {
        public ServiceProductMessage(ServiceProductDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
