using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Route;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class CreateManyWarehouseRoute : CallActionWithBodyRequestResultBase<List<WarehouseRouteSimpleDto>, ManyWarehouseRouteCreateDto>
    {
        public CreateManyWarehouseRoute(ManyWarehouseRouteCreateDto dto)
            : base(dto, ApiResources.Warehouses, "routes/create_many")
        {
        }
    }
}