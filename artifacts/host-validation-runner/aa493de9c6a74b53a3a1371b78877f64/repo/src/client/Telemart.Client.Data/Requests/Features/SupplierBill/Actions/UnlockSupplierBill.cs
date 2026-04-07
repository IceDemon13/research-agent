using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill.Actions
{
    public sealed class UnlockSupplierBill : UnlockRequestBase<SupplierBillDto>
    {
        public UnlockSupplierBill(int id, bool force = false)
            : base(force, ApiResources.SupplierBills, id)
        {
        }
    }
}
