using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public sealed class CreateManyPerformances : CallActionWithBodyRequestResultBase<List<WarehousePerformanceDto>, WarehouseManyPerfomancesSaveDto>
    {
        public CreateManyPerformances(int warehouseId, WarehouseManyPerfomancesSaveDto dto)
            : base(dto, $"{ApiResources.Warehouses}", $"{ApiResources.Perfomances}/create_many")
        {
        }
    }
}