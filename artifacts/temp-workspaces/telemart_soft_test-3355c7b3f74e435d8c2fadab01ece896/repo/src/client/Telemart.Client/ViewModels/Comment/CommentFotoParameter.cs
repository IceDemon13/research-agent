namespace Telemart.Client.ViewModels.Comment
{
    public sealed class CommentFotoParameter
    {
        public CommentFotoParameter(int commentId)
        {
            CommentId = commentId;
        }

        public int CommentId { get; }
    }
}