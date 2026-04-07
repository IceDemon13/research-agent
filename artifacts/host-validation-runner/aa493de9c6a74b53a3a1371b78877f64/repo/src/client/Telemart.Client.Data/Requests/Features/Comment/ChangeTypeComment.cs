using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Data.Requests.Features.Comment
{
    public class ChangeTypeComment : UpdateEntityResultRequestBase<CommentFullDto, ChangeTypeComment.CommentChangeTypeDto>
    {
        public ChangeTypeComment(int commentId, int typeId)
            : base(new CommentChangeTypeDto(typeId), ApiResources.Comments, commentId, "type")
        {
        }

        public class CommentChangeTypeDto
        {
            public CommentChangeTypeDto(int typeId)
            {
                TypeId = typeId;
            }

            [JsonProperty("type_id")]
            public int TypeId { get; set; }
        }
    }
}