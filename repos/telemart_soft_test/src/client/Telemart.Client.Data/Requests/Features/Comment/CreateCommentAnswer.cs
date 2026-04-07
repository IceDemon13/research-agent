using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class CreateCommentAnswer : CreateEntityResultRequestBase<CommentSimpleDto, CommentCreateAnswerDto>
    {
        public CreateCommentAnswer(int parentId, CommentCreateAnswerDto dto)
            : base(dto, ApiResources.Comments, parentId.ToString(), "actions", "answer")
        {
            SuccessStatusCode = HttpStatusCode.OK;
        }
    }
}