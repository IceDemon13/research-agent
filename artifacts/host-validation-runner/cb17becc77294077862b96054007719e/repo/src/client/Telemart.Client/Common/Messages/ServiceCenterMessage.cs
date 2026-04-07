using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceCenterMessage : EntityMessage<ServiceCenterDto>
    {
        public ServiceCenterMessage(ServiceCenterDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
