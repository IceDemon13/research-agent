using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class QueryPerformance : QueryEntityRequestBase<WarehousePerformanceDto>
    {
        public QueryPerformance(int warehouseId, int performanceId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Perfomances, performanceId)
        {
        }
    }
}