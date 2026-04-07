using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Common.Messages
{
    public class WarehouseDeliveryMessage : EntityMessage<DeliveryDto>
    {
        public WarehouseDeliveryMessage(DeliveryDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
