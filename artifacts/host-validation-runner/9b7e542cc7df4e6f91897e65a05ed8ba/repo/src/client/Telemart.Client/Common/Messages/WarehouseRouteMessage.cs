using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Common.Messages
{
    public class WarehouseRouteMessage : EntityMessage<WarehouseRouteSimpleDto>
    {
        public WarehouseRouteMessage(WarehouseRouteSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
