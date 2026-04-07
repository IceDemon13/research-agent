using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Common.Messages
{
    public class CarryMessage : EntityMessage<CarryDto>
    {
        public CarryMessage(CarryDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
