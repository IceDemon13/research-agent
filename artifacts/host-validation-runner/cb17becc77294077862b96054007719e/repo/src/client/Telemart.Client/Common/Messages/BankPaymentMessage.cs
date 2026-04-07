using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Common.Messages
{
    public class BankPaymentMessage : EntityMessage<BankPaymentDto>
    {
        public BankPaymentMessage(BankPaymentDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
