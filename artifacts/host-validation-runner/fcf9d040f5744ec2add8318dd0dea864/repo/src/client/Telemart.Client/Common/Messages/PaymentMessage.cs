using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Common.Messages
{
    public class PaymentMessage : EntityMessage<PaymentDto>
    {
        public PaymentMessage(PaymentDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
