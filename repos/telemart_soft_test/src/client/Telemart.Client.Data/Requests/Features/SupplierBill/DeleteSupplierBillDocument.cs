using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.SupplierBill
{
    public class DeleteSupplierBillDocument : DeleteEntityResultRequestBase<object>
    {
        public DeleteSupplierBillDocument(int id)
            : base(ApiResources.SupplierBills, "documents", id)
        {
        }
    }
}