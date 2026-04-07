using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class UpdateCategoryOptions : UpdateEntityResultRequestBase<CategoryFullDto, CategoryOptionsSaveDto>
    {
        public UpdateCategoryOptions(int categoryId, CategoryOptionsSaveDto dto)
            : base(dto, ApiResources.Categories, categoryId, "options")
        {
        }
    }
}