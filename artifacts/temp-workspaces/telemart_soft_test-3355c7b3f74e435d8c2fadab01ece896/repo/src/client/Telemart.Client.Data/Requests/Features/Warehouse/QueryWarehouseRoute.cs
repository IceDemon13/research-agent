using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class QueryWarehouseRoute : QueryEntityRequestBase<WarehouseRouteFullDto>
    {
        public QueryWarehouseRoute(int warehouseId, int routeId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Routes, routeId)
        {
        }
    }
}