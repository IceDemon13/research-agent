using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class UnlockServiceMovement : UnlockRequestBase<ServiceMovementDto>
    {
        public UnlockServiceMovement(int id, bool force = false)
            : base(force, ApiResources.ServiceMovements, id)
        {
        }
    }
}
