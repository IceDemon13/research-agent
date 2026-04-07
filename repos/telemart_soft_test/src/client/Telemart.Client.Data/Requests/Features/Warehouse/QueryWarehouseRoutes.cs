using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class QueryWarehouseRoutes : QueryEntitiesRequestBase<WarehouseRouteSimpleDto>
    {
        public QueryWarehouseRoutes(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Routes)
        {
        }
    }
}