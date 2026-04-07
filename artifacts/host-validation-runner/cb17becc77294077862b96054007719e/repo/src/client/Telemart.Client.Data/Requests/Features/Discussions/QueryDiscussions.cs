using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class QueryDiscussions : QueryEntitiesPagedRequestBase<DiscussionDto>
    {
        public QueryDiscussions(IFilteringItem filter)
            : base(filter, ApiResources.Discussions)
        {
        }
    }
}