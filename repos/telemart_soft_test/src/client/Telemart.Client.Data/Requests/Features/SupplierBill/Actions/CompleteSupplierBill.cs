using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill.Actions
{
    public sealed class CompleteSupplierBill : CallEntityActionRequestResultBase<SupplierBillDto>
    {
        public CompleteSupplierBill(int id)
            : base(id, ApiResources.SupplierBills, "complete")
        {
        }
    }
}
