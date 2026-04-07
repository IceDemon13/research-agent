using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Hashtag
{
    public sealed class QueryHashtags : QueryEntitiesRequestBase<HashtagDto>
    {
        public QueryHashtags()
            : base(ApiResources.Hashtags)
        {
        }
    }
}
