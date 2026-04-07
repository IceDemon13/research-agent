namespace Telemart.Client.ViewModels.Base
{
    public sealed class CommentEditorParameter : EditorParameter
    {
        public CommentEditorParameter(int entityId, string comment)
            : base(entityId)
        {
            Comment = comment;
        }

        public string Comment { get; }
    }
}