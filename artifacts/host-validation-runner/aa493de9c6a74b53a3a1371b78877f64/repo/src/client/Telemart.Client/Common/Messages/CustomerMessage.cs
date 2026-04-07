using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Common.Messages
{
    public class CustomerMessage : EntityMessage<CustomerDto>
    {
        public CustomerMessage(CustomerDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
