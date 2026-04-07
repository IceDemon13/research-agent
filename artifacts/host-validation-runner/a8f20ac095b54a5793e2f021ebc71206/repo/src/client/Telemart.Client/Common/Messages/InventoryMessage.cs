using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class InventoryMessage : EntityMessage<InventoryDto>
    {
        public InventoryMessage(InventoryDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
