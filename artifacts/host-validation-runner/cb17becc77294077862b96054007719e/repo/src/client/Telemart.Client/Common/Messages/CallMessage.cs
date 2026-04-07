using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Common.Messages
{
    public sealed class CallMessage : EntityMessage<CallDto>
    {
        public CallMessage(CallDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
