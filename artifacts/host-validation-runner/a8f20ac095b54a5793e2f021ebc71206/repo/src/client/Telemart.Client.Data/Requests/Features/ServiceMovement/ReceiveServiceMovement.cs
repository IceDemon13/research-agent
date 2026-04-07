using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class ReceiveServiceMovement : CallEntityActionWithBodyRequestResultBase<ServiceMovementDto, ServiceMovementReceiveDto>
    {
        public ReceiveServiceMovement(int id, ServiceMovementReceiveDto dto)
            : base(id, dto, ApiResources.ServiceMovements, "receive")
        {
        }
    }
}