using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public sealed class QueryAutoShowcases : QueryEntitiesPagedRequestBase<AutoShowcaseDto>
    {
        public QueryAutoShowcases(IFilteringItem filter)
            : base(filter, $"{ApiResources.Showcases}/auto")
        {
        }
    }
}