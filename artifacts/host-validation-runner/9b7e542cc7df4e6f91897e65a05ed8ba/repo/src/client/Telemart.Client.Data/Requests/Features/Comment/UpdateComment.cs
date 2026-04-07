using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class UpdateComment : UpdateEntityResultRequestBase<CommentSimpleDto, CommentSaveDto>
    {
        public UpdateComment(int id, CommentSaveDto dto)
            : base(dto, ApiResources.Comments, id)
        {
        }
    }
}
