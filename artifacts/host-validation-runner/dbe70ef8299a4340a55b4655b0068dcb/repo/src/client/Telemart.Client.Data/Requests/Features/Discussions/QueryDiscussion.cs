using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Discussions;

namespace Telemart.Client.Data.Requests.Features.Discussions
{
    public sealed class QueryDiscussion : QueryEntityRequestBase<DiscussionDto>
    {
        public QueryDiscussion(int id)
            : base(ApiResources.Discussions, id)
        {
        }
    }
}