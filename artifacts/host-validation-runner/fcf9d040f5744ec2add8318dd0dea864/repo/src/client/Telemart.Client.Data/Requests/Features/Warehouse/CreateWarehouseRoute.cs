using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class CreateWarehouseRoute : CreateEntityResultRequestBase<WarehouseRouteSimpleDto, WarehouseRouteCreateDto>
    {
    public CreateWarehouseRoute(int warehouseId, WarehouseRouteCreateDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Routes)
        {
        }
    }
}