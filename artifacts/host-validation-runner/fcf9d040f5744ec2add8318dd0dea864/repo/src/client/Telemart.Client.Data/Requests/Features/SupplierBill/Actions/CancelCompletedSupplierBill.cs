using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill.Actions
{
    public sealed class CancelCompletedSupplierBill : CallEntityActionRequestResultBase<SupplierBillDto>
    {
        public CancelCompletedSupplierBill(int id)
            : base(id, ApiResources.SupplierBills, "cancel_completed")
        {
        }
    }
}