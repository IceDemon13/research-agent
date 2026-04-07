using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class QueryDiscussionTypes : QueryEntitiesRequestBase<DiscussionTypeDto>
    {
        public QueryDiscussionTypes()
            : base(ApiResources.Discussions, "types")
        {
        }
    }
}