using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class QueryWarehousePerformancePatterns : QueryEntitiesRequestBase<WarehousePerformancePatternDto>
    {
        public QueryWarehousePerformancePatterns()
            : base(ApiResources.Warehouses,  "performance_patterns")
        {
        }
    }
}