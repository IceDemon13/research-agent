using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class QueryContractorWarehouses : QueryEntitiesRequestBase<SupplierWarehouseDto>
    {
        public QueryContractorWarehouses(int contractorId)
            : base(ApiResources.Contractors, contractorId, "warehouses")
        {
        }
    }
}