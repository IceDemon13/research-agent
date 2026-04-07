namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceSupplierReportProductData
    {
        public ReturnInvoiceSupplierReportProductData(int productId, string productName, int quantity)
        {
            ProductId = productId;
            ProductName = productName;
            Quantity = quantity;
        }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }
    }
}
