using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRepairMessage : EntityMessage<ServiceRepairDto>
    {
        public ServiceRepairMessage(ServiceRepairDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}