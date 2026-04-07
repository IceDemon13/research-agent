using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public class UpdateDelivery : UpdateEntityResultRequestBase<DeliveryDto, DeliverySaveDto>
    {
        public UpdateDelivery(int warehouseId, int deliveryId, DeliverySaveDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Deliveries, deliveryId)
        {
        }
    }
}