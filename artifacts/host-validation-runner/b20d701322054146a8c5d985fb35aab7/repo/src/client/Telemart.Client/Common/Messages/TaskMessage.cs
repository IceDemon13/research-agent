using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Common.Messages
{
    public class TaskMessage : EntityMessage<TaskDto>
    {
        public TaskMessage(TaskDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
