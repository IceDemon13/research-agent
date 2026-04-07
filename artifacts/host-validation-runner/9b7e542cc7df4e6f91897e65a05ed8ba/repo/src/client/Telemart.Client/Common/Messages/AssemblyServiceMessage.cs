using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Common.Messages
{
    public class AssemblyServiceMessage : EntityMessage<AssemblyServiceDto>
    {
        public AssemblyServiceMessage(AssemblyServiceDto dto, MessageType messageType)
            : base(dto, messageType)
        {
        }
    }
}
