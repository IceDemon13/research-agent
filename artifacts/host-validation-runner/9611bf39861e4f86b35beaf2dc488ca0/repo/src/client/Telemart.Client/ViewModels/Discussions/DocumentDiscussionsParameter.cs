namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DocumentDiscussionsParameter
    {
        public DocumentDiscussionsParameter(int entityId, int documentId)
        {
            EntityId = entityId;
            DocumentId = documentId;
        }

        public int EntityId { get; }

        public int DocumentId { get; }
    }
}