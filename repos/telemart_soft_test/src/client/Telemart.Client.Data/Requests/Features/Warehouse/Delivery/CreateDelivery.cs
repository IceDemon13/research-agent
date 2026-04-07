using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public class CreateDelivery : CreateEntityResultRequestBase<DeliveryDto, DeliverySaveDto>
    {
        public CreateDelivery(int warehouseId, DeliverySaveDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Deliveries)
        {
        }
    }
}