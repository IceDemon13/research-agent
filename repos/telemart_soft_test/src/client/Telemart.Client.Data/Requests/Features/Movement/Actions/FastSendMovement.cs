using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public sealed class FastSendMovement : CallEntityActionWithBodyRequestResultBase<MovementDto, MovementFastSendDto>
    {
        public FastSendMovement(int movementId, bool fastSale = false, bool system = false)
            : base(movementId, new MovementFastSendDto(fastSale, system), ApiResources.Movements, "fast_send")
        {
        }
    }
}