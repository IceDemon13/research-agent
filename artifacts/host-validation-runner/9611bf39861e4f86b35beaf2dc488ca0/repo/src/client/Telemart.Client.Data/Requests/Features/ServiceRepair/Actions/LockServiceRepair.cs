using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair.Actions
{
    public sealed class LockServiceRepair : LockRequestBase<ServiceRepairDto>
    {
        public LockServiceRepair(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceRepairs, id)
        {
        }
    }
}
