using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class ChangeStateComment : UpdateEntityResultRequestBase<CommentSimpleDto, CommentChangeStateDto>
    {
        public ChangeStateComment(int commentId, CommentChangeStateDto dto)
            : base(dto, ApiResources.Comments, commentId, "state")
        {
        }
    }
}