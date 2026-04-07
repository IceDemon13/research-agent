using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Category
{
    public sealed class QueryCategoryFull : QueryEntityRequestBase<CategoryFullDto>
    {
        public QueryCategoryFull(int categoryId)
            : base(ApiResources.Categories, categoryId, "options")
        {
        }
    }
}