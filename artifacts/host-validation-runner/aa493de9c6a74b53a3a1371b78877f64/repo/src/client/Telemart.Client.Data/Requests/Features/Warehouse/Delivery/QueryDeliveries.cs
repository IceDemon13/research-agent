using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public class QueryDeliveries : QueryEntitiesRequestBase<DeliveryDto>
    {
        public QueryDeliveries(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Deliveries)
        {
        }
    }
}