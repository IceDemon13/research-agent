using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.SupplierBill;

namespace Telemart.Client.Data.Requests.Features.SupplierBill.Actions
{
    public sealed class LockSupplierBill : LockRequestBase<SupplierBillDto>
    {
        public LockSupplierBill(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.SupplierBills, id)
        {
        }
    }
}
