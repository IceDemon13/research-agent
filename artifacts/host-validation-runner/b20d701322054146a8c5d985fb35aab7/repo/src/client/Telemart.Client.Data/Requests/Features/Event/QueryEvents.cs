using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Event
{
    public sealed class QueryEvents : QueryEntitiesRequestBase<EventDto>
    {
        public QueryEvents(IFilteringItem filter)
            : base(filter, ApiResources.Events)
        {
        }
    }
}