using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class QueryContractorsWarehouses : QueryEntitiesRequestBase<SupplierWarehouseDto>
    {
        public QueryContractorsWarehouses()
            : base($"{ApiResources.Contractors}/warehouses")
        {
        }
    }
}