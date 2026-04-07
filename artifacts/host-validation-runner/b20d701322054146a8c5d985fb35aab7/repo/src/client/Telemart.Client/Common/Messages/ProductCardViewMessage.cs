namespace Telemart.Client.Common.Messages
{
    public class ProductCardViewMessage
    {
        public ProductCardViewMessage(int productId)
        {
            ProductId = productId;
        }

        public int ProductId { get; }
    }
}
