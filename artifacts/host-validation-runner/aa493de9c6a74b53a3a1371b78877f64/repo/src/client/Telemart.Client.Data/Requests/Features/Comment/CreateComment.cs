using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class CreateComment : CreateEntityResultRequestBase<CommentSimpleDto, CommentCreateDto>
    {
        public CreateComment(CommentCreateDto dto)
            : base(dto, ApiResources.Comments)
        {
        }
    }
}
