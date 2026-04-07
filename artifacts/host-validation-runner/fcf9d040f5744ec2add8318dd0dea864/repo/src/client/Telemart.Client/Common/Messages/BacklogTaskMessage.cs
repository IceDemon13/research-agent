using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Common.Messages
{
    public sealed class BacklogTaskMessage : EntityMessage<BacklogTaskDto>
    {
        public BacklogTaskMessage(BacklogTaskDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
