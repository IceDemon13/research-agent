using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class QueryShowcases : QueryEntitiesPagedRequestBase<ShowcaseDto>
    {
        public QueryShowcases(IFilteringItem filter)
            : base(filter, ApiResources.Showcases)
        {
        }
    }
}
