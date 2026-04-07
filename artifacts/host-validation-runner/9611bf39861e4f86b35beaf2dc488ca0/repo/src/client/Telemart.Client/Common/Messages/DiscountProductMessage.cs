using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class DiscountProductMessage : EntityMessage<ProductCardDto>
    {
        public DiscountProductMessage(ProductCardDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
