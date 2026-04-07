namespace Telemart.Client.ViewModels.Base
{
    public abstract class EditorParameter : IEditorParameter
    {
        protected EditorParameter(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public bool IsNew => Id == 0;
    }
}