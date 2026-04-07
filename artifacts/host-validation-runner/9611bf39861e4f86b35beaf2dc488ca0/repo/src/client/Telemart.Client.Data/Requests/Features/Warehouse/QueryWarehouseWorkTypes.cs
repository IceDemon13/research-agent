using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class QueryWarehouseWorkTypes : QueryEntitiesRequestBase<WarehouseWorkTypeDto>
    {
        public QueryWarehouseWorkTypes()
            : base($"{ApiResources.Warehouses}/{ApiResources.WorkTypes}")
        {
        }
    }
}
