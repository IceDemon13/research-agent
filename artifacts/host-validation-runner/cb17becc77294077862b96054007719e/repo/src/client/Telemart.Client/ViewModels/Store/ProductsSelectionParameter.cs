namespace Telemart.Client.ViewModels.Store
{
    public class ProductsSelectionParameter
    {
        public ProductsSelectionParameter(int orderId, bool separateWarrantyCards)
        {
            SeparateWarrantyCards = separateWarrantyCards;
            OrderId = orderId;
        }

        public bool SeparateWarrantyCards { get; }

        public int OrderId { get; }
    }
}
