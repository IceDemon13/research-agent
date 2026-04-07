using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class QueryDiscussionHashtags : QueryEntitiesRequestBase<DiscussionHashtagDto>
    {
        public QueryDiscussionHashtags()
            : base(ApiResources.Discussions, "hashtags")
        {
        }
    }
}