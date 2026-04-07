using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class MoveCategory : CallEntityActionWithBodyRequestResultBase<object, CategoryMoveDto>
    {
        public MoveCategory(int id, int targetId)
            : base(id, new CategoryMoveDto(id, targetId), ApiResources.Categories, "move")
        {
        }
    }
}