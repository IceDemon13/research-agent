using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class QueryPerformances : QueryEntitiesRequestBase<WarehousePerformanceDto>
    {
        public QueryPerformances(int warehouseId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Perfomances)
        {
        }
    }
}