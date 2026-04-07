using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Debezium;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public sealed class QueryShowcaseHistoriesByFilter : QueryEntitiesRequestBase<ShowcaseHistoryDto>
    {
        public QueryShowcaseHistoriesByFilter(IFilteringItem filter)
            : base(filter, ApiResources.Showcases, "showcase_history_by_period")
        {
        }
    }
}