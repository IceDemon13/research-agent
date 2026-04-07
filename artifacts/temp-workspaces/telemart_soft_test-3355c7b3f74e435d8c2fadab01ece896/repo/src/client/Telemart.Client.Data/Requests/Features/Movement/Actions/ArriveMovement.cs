using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement.Actions
{
    public class ArriveMovement : CallEntityActionRequestResultBase<MovementDto>
    {
        public ArriveMovement(int id)
            : base(id, ApiResources.Movements, "arrive")
        {
        }
    }
}