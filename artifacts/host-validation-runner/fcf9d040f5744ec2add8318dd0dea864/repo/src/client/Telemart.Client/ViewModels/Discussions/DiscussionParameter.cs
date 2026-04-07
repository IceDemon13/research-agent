using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionParameter : EditorParameter
    {
        public DiscussionParameter(int id, int entityId, int documentId, bool showAllTypes = false, int? typeId = null)
            : base(id)
        {
            DocumentId = documentId;
            EntityId = entityId;
            ShowAllTypes = showAllTypes;
            TypeId = typeId;
        }

        public int EntityId { get; }

        public int DocumentId { get; }

        public bool ShowAllTypes { get; }

        public int? TypeId { get; set; }
    }
}