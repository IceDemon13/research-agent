using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class UpdatePerformance : UpdateEntityResultRequestBase<WarehousePerformanceDto, WarehousePerfomanceSaveDto>
    {
        public UpdatePerformance(int warehouseId, int performanceId, WarehousePerfomanceSaveDto dto)
            : base(dto, ApiResources.Warehouses, warehouseId, ApiResources.Perfomances, performanceId)
        {
        }
    }
}