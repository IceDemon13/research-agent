using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class PurchaseMessage : EntityMessage<PurchaseDto>
    {
        public PurchaseMessage(PurchaseDto purchase, MessageType messageType)
            : base(purchase, messageType)
        {
        }
    }
}