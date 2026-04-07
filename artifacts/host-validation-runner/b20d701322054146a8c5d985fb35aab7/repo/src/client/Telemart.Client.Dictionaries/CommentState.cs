namespace Telemart.Client.Dictionaries
{
    public class CommentState : DictionaryItem
    {
        public const int NewId = 1;
        public const int ActiveId = 2;
        public const int HideId = 3;

        private CommentState(int id, string name)
            : base(id, name, true)
        {
        }

        public static CommentState New { get; } = new CommentState(NewId, "Новый");

        public static CommentState ActiveState { get; } = new CommentState(ActiveId, "Активен");

        public static CommentState Hide { get; } = new CommentState(HideId, "Скрыт");
    }
}
