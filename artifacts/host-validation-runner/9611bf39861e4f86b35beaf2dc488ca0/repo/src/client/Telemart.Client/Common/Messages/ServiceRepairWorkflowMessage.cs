using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRepairWorkflowMessage
    {
        public ServiceRepairWorkflowMessage(ServiceRepairDto entity)
        {
            Entity = entity;
        }

        public ServiceRepairDto Entity { get; }
    }
}