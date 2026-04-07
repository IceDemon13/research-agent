using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class QueryWarehouseFullRoutes : QueryEntitiesRequestBase<WarehouseRouteFullDto>
    {
        public QueryWarehouseFullRoutes(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, "full_routes")
        {
        }
    }
}