using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class CancelMovement : CallEntityActionRequestResultBase<MovementDto>
    {
        public CancelMovement(int movementId)
            : base(movementId, ApiResources.Movements, "cancel")
        {
        }
    }
}