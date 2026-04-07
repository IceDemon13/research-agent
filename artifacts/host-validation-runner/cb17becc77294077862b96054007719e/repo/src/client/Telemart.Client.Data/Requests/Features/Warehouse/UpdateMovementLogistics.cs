using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class UpdateMovementLogistics : CallEntityActionWithBodyRequestResultBase<MovementDto, UpdateMovementLogisticsDto>
    {
        public UpdateMovementLogistics(int id, int carryId, string ttn)
            : base(id, new UpdateMovementLogisticsDto(id, carryId, ttn), ApiResources.Movements, "set_logistics")
        {
        }
    }
}
