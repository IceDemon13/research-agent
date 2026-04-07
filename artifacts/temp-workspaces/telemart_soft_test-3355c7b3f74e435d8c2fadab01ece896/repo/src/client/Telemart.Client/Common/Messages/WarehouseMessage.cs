using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Common.Messages
{
    public class WarehouseMessage : EntityMessage<WarehouseDto>
    {
        public WarehouseMessage(WarehouseDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
