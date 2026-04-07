using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class RefundMessage : EntityMessage<RefundDto>
    {
        public RefundMessage(RefundDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
