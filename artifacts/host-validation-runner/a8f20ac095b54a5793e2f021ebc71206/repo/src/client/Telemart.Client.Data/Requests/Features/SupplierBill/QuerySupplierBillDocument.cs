using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class QuerySupplierBillDocument : QueryEntityRequestBase<SupplierBillDocumentDto>
    {
        public QuerySupplierBillDocument(object id)
            : base(ApiResources.SupplierBills, "documents", id)
        {
        }
    }
}