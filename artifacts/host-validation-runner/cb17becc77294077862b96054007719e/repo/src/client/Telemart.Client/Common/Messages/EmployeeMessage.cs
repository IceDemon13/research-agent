using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class EmployeeMessage : EntityMessage<EmployeeDto>
    {
        public EmployeeMessage(EmployeeDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}