namespace Telemart.Client.Common.Messages
{
    public class CallDependencyWarningMessage
    {
        public CallDependencyWarningMessage(int? dependencyTypeId, string name, int? callId, int? documentId, string comment)
        {
            Name = name;
            CallId = callId;
            DependencyTypeId = dependencyTypeId;
            DocumentId = documentId;
            Comment = comment;
        }

        public string Name { get; }

        public int? CallId { get; }

        public int? DependencyTypeId { get; }

        public int? DocumentId { get; }

        public string Comment { get; }
    }
}