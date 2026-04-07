namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class GenerateSerialNumbersParameter
    {
        public GenerateSerialNumbersParameter(int productId, string productName)
        {
            ProductId = productId;
            ProductName = productName;
        }

        public int ProductId { get; }

        public string ProductName { get; }
    }
}
