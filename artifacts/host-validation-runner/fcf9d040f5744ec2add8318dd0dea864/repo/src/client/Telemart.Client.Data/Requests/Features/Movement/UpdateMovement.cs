using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class UpdateMovement : UpdateEntityResultRequestBase<MovementDto, MovementUpdateDto>
    {
        public UpdateMovement(int movementId, MovementUpdateDto dto)
            : base(dto, ApiResources.Movements, movementId)
        {
        }
    }
}
