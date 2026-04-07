using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class UpdateWarehouseRoute : UpdateEntityResultRequestBase<WarehouseRouteFullDto, WarehouseRouteSaveDto>
    {
        public UpdateWarehouseRoute(int warehouseId, int routeId, WarehouseRouteSaveDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Routes, routeId)
        {
        }
    }
}