using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class MovementMessage : EntityMessage<MovementDto>
    {
        public MovementMessage(MovementDto dto, MessageType messageType)
            : base(dto, messageType)
        {
        }
    }
}