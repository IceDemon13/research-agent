using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Common.Messages
{
    public class WarehousePerfomanceMessage : EntityMessage<WarehousePerformanceDto>
    {
        public WarehousePerfomanceMessage(WarehousePerformanceDto entity, MessageType messageType)
          : base(entity, messageType)
        {
        }
    }
}
