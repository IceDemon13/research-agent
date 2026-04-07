using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public class QueryDelivery : QueryEntityRequestBase<DeliveryDto>
    {
        public QueryDelivery(int warehouseId, int deliveryId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Deliveries, deliveryId)
        {
        }
    }
}