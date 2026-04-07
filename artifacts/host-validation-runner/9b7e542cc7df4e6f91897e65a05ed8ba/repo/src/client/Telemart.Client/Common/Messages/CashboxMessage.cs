using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Common.Messages
{
    public class CashboxMessage : EntityMessage<CashboxDto>
    {
        public CashboxMessage(CashboxDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
