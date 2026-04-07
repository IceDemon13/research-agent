using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class QuerySupplierBillDocuments : QueryEntitiesRequestBase<SupplierBillDocumentDto>
    {
        public QuerySupplierBillDocuments(int supplierBillId)
            : base(ApiResources.SupplierBills, supplierBillId, "documents")
        {
        }
    }
}