using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ContractorMessage : EntityMessage<ContractorDto>
    {
        public ContractorMessage(ContractorDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
