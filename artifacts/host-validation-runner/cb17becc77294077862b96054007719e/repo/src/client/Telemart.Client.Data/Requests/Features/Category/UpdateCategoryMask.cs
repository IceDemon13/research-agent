using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class UpdateCategoryMask : UpdateEntityResultRequestBase<CategoryFullDto, CategoryMaskSaveDto>
    {
        public UpdateCategoryMask(int categoryId, string mask, int languageId)
            : base(new CategoryMaskSaveDto(categoryId, mask, languageId), ApiResources.Categories, categoryId, "mask")
        {
        }
    }
}