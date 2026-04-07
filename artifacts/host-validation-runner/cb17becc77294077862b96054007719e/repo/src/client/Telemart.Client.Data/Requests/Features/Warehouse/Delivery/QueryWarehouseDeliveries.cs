using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public sealed class QueryWarehouseDeliveries : QueryEntitiesRequestBase<DeliveryDto>
    {
        public QueryWarehouseDeliveries()
            : base($"{ApiResources.Warehouses}/{ApiResources.Deliveries}")
        {
        }
    }
}