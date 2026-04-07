using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public class ProductBarcodeMessage : EntityMessage<ProductBarcodeDto>
    {
        public ProductBarcodeMessage(ProductBarcodeDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
