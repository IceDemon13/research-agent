using Telemart.Client.TransferObjects.PromoCode;

namespace Telemart.Client.Common.Messages
{
    public class PromoCodeMessage : EntityMessage<PromoCodeFullDto>
    {
        public PromoCodeMessage(PromoCodeFullDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
