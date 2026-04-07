using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class DeleteWarehouseRoute : DeleteEntityResultRequestBase<object>
    {
        public DeleteWarehouseRoute(int warehouseId, int routeId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Routes, routeId)
        {
        }
    }
}