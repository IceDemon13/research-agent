namespace Telemart.Client.Dictionaries
{
    public class CommentType : DictionaryItem
    {
        public const int CommentId = 1;
        public const int QuestionId = 2;

        private CommentType(int id, string name)
            : base(id, name, true)
        {
        }

        public static CommentType Comment { get; } = new CommentType(CommentId, "Комментарий");

        public static CommentType Question { get; } = new CommentType(QuestionId, "Вопрос");
    }
}
