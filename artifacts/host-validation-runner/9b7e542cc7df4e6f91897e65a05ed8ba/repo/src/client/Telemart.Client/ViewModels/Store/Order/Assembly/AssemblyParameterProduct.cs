namespace Telemart.Client.ViewModels.Store.Order.Assembly
{
    public class AssemblyParameterProduct
    {
        public AssemblyParameterProduct(int productId, int quantity, bool readOnly, bool assemblyIncluded, bool isGift, decimal price)
        {
            ProductId = productId;
            Quantity = quantity;
            ReadOnly = readOnly;
            AssemblyIncluded = assemblyIncluded;
            IsGift = isGift;
            Price = price;
        }

        public int ProductId { get; }

        public bool ReadOnly { get; }

        public int Quantity { get; }

        public bool AssemblyIncluded { get; }

        public bool IsGift { get; }

        public decimal Price { get; }
    }
}
