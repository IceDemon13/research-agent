using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public class CancelServiceMovement : CallEntityActionRequestResultBase<ServiceMovementDto>
    {
        public CancelServiceMovement(int id)
            : base(id, ApiResources.ServiceMovements, "cancel")
        {
        }
    }
}
