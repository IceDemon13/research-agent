using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class QuerySupplierBill : QueryEntityRequestBase<SupplierBillDto>
    {
        public QuerySupplierBill(object id)
            : base(ApiResources.SupplierBills, id)
        {
        }
    }
}