using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class LockMovement : LockRequestBase<MovementDto>
    {
        public LockMovement(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Movements, id)
        {
        }
    }
}