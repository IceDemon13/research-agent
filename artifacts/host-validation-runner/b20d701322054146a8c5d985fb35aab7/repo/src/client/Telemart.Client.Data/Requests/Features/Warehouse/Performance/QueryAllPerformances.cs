using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Perfomance;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class QueryAllPerformances : QueryEntitiesRequestBase<WarehousePerformanceDto>
    {
        public QueryAllPerformances()
            : base(ApiResources.Warehouses, ApiResources.Perfomances)
        {
        }
    }
}