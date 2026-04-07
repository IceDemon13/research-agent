using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class LockRefund : LockRequestBase<RefundDto>
    {
        public LockRefund(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Refunds, id)
        {
        }
    }
}
