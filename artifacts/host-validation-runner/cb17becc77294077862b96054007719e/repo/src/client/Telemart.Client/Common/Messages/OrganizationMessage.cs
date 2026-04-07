using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class OrganizationMessage : EntityMessage<OrganizationDto>
    {
        public OrganizationMessage(OrganizationDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
