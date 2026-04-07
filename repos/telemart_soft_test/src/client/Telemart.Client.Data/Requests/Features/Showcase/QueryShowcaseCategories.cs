using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class QueryShowcaseCategories : QueryEntitiesRequestBase<ShowcaseCategoryDto>
    {
        public QueryShowcaseCategories()
            : base($"{ApiResources.Showcases}/categories")
        {
        }
    }
}