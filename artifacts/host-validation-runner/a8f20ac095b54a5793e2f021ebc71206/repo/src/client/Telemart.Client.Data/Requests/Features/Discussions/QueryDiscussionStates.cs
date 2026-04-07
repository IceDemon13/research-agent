using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class QueryDiscussionStates : QueryEntitiesRequestBase<DiscussionStateDto>
    {
        public QueryDiscussionStates()
            : base(ApiResources.Discussions, "states")
        {
        }
    }
}