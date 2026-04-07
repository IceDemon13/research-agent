using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class ReorderCategoryChildren : CallEntityActionWithBodyRequestResultBase<object, CategoryReorderChildrenDto>
    {
        public ReorderCategoryChildren(CategoryReorderChildrenDto dto)
            : base(dto.Id, dto, ApiResources.Categories, "reorder")
        {
        }
    }
}