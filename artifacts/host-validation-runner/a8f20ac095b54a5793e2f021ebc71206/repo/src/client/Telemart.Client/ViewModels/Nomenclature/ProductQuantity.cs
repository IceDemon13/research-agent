namespace Telemart.Client.ViewModels.Nomenclature
{
    public class ProductQuantity
    {
        public ProductQuantity(int productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        public int ProductId { get; }

        public int Quantity { get; }
    }
}
