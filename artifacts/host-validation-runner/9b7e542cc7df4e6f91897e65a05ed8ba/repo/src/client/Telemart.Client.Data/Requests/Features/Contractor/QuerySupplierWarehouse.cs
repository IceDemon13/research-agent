using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class QuerySupplierWarehouse : QueryEntityRequestBase<SupplierWarehouseDto>
    {
        public QuerySupplierWarehouse(int supplierWarehouseId)
            : base(supplierWarehouseId, $"{ApiResources.Contractors}/warehouse")
        {
        }
    }
}