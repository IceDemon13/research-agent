namespace Telemart.Client.Common.Messages
{
    public class ShowNewCommentNotificationMessage
    {
        public ShowNewCommentNotificationMessage(int commentId, string caption, string content)
        {
            CommentId = commentId;
            Caption = caption;
            Content = content;
        }

        public int CommentId { get; }

        public string Caption { get; }

        public string Content { get; }
    }
}