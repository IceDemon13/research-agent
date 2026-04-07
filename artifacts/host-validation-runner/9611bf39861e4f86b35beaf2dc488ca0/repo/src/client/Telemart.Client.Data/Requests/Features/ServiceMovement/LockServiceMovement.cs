using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class LockServiceMovement : LockRequestBase<ServiceMovementDto>
    {
        public LockServiceMovement(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceMovements, id)
        {
        }
    }
}
