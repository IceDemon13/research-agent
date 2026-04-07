using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class UnlockMovement : UnlockRequestBase<MovementDto>
    {
        public UnlockMovement(int id, bool force = false)
            : base(force, ApiResources.Movements, id)
        {
        }
    }
}