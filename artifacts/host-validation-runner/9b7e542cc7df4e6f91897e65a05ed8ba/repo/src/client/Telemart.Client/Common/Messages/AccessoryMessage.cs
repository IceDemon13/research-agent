using Telemart.Client.TransferObjects.Accessory;

namespace Telemart.Client.Common.Messages
{
    public class AccessoryMessage : EntityMessage<AccessoryDto>
    {
        public AccessoryMessage(AccessoryDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
