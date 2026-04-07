using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Common.Messages
{
    public class CarryPriceMessage : EntityMessage<CarryPriceDto>
    {
        public CarryPriceMessage(CarryPriceDto entity, MessageType messageType)
          : base(entity, messageType)
        {
        }
    }
}
