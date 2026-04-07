using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry.Actions
{
    public class ReorderCarries : CallActionWithBodyRequestResultBase<object, CarryPositionDto[]>
    {
        public ReorderCarries(CarryPositionDto[] dto)
            : base(dto, ApiResources.Carries, "reorder")
        {
        }
    }
}