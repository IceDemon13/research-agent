using Telemart.Client.Common.Messages;

namespace Telemart.Client.Common
{
    public sealed class CallDependencyMessage
    {
        public CallDependencyMessage(MessageType messageType, int? dependencyTypeId, string name, string comment, int? callId, int? documentId)
        {
            Name = name;
            Comment = comment;
            CallId = callId;
            DependencyTypeId = dependencyTypeId;
            MessageType = messageType;
            DocumentId = documentId;
        }

        public MessageType MessageType { get; }

        public string Name { get; }

        public string Comment { get; }

        public int? CallId { get; }

        public int? DependencyTypeId { get; }

        public int? DocumentId { get; }
    }
}