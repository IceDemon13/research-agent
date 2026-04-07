using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public class ProductDimensionsMessage : EntityMessage<ProductDimensionsDto>
    {
        public ProductDimensionsMessage(ProductDimensionsDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
