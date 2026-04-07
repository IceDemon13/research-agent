using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class QueryComments : QueryEntitiesPagedRequestBase<CommentSimpleDto>
    {
        public QueryComments(IFilteringItem filter)
            : base(filter, ApiResources.Comments)
        {
        }
    }
}
