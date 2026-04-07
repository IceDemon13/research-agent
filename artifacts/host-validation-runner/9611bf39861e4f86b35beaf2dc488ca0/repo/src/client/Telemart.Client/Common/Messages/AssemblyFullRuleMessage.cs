using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Common.Messages
{
    public class AssemblyFullRuleMessage : EntityMessage<AssemblyFullRuleDto>
    {
        public AssemblyFullRuleMessage(AssemblyFullRuleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
