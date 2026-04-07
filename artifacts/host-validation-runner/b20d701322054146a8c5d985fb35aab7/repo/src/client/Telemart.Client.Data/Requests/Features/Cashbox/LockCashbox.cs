using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox
{
    public class LockCashbox : LockRequestBase<CashboxDto>
    {
        public LockCashbox(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Cashboxes, id)
        {
        }
    }
}