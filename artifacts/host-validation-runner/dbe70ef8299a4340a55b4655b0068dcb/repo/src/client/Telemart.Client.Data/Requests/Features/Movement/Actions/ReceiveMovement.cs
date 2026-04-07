using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class ReceiveMovement : CallEntityActionRequestResultBase<MovementDto>
    {
        public ReceiveMovement(int movementId, bool system = false)
        : base(movementId, ApiResources.Movements, $"receive/{system}")
        {
        }
    }
}