using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public sealed class QuerySupplierBills : QueryEntitiesPagedRequestBase<SupplierBillDto>
    {
        public QuerySupplierBills(IFilteringItem filter)
            : base(filter, ApiResources.SupplierBills)
        {
        }
    }
}
