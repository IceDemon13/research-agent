using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Common.Messages
{
    public class ServiceMovementMessage : EntityMessage<ServiceMovementSimpleDto>
    {
        public ServiceMovementMessage(ServiceMovementSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
