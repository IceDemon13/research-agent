using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class QueryComment : QueryEntityRequestBase<CommentFullDto>
    {
        public QueryComment(int id)
            : base(ApiResources.Comments, id)
        {
        }
    }
}