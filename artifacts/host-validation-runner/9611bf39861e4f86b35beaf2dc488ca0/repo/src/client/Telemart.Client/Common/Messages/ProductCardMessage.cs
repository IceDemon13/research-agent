using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ProductCardMessage : EntityMessage<ProductCardDto>
    {
        public ProductCardMessage(ProductCardDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
