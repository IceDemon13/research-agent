using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class CreateCategory : CreateEntityResultRequestBase<CategoryDto, CategoryCreateDto>
    {
        public CreateCategory(int parentId, string name, string nameUkr, string nameEn, string linkRewrite, CategoryCreateType type)
            : base(
                new CategoryCreateDto(name, nameUkr, nameEn, linkRewrite, type),
                ApiResources.Categories,
                parentId.ToString(),
                "children")
        {
        }
    }
}