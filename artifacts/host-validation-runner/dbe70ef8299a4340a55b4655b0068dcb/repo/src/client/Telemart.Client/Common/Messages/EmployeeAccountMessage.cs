using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.Common.Messages
{
    public class EmployeeAccountMessage : EntityMessage<EmployeeAccountDto>
    {
        public EmployeeAccountMessage(EmployeeAccountDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
