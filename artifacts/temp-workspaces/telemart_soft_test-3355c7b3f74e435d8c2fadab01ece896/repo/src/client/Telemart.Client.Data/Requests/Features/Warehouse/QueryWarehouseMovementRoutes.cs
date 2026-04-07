using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class QueryWarehouseMovementRoutes : QueryEntitiesRequestBase<MovementRouteDto>
    {
        public QueryWarehouseMovementRoutes(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, "movement", "routes")
        {
        }
    }
}