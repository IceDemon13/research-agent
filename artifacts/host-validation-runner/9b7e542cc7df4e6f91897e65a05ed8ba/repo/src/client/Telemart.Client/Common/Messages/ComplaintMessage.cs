using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Common.Messages
{
    public class ComplaintMessage : EntityMessage<ComplaintDto>
    {
        public ComplaintMessage(ComplaintDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
