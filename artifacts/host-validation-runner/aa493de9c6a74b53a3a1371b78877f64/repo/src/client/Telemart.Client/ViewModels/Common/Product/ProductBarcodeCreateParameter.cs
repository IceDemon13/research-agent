namespace Telemart.Client.ViewModels.Common.Product
{
    public class ProductBarcodeCreateParameter
    {
        public ProductBarcodeCreateParameter(int productId)
        {
            ProductId = productId;
        }

        public int ProductId { get; set; }
    }
}
