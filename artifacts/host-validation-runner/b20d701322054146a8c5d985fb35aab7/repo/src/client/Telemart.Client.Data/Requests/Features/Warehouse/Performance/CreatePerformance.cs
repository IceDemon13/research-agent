using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class CreatePerformance : CreateEntityResultRequestBase<WarehousePerformanceDto, WarehousePerfomanceSaveDto>
    {
        public CreatePerformance(int warehouseId, WarehousePerfomanceSaveDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Perfomances)
        {
        }
    }
}