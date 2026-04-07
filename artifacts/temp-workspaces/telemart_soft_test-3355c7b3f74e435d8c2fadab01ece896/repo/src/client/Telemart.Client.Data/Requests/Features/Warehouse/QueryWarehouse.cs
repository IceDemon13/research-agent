using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public class QueryWarehouse : QueryEntityRequestBase<WarehouseDto>
    {
        public QueryWarehouse(int id)
            : base(ApiResources.Warehouses, id)
        {
        }
    }
}