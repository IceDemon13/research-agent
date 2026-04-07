using Telemart.Client.TransferObjects.Comment;

namespace Telemart.Client.Common.Messages
{
    public class CommentMessage : EntityMessage<CommentSimpleDto>
    {
        public CommentMessage(CommentSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
