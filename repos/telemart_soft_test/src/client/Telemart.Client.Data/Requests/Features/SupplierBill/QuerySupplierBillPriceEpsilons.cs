using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public sealed class QuerySupplierBillPriceEpsilons : QueryRequestBase<PriceEpsilonsDto>
    {
        public QuerySupplierBillPriceEpsilons()
            : base(ApiResources.SupplierBills, "price_epsilons")
        {
        }
    }
}