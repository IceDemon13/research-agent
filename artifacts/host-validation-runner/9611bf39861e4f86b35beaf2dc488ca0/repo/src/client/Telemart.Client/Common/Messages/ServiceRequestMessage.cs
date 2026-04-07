using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRequestMessage : EntityMessage<ServiceRequestDto>
    {
        public ServiceRequestMessage(ServiceRequestDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}