using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public sealed class LockCall : LockRequestBase<CallDto>
    {
        public LockCall(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Calls, id)
        {
        }
    }
}