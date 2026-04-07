namespace Telemart.Client.Reports.Inventory
{
    public class LeftoversProductReportData
    {
        public LeftoversProductReportData(int productId, string nameFull, int quantity, int freeQuantity)
        {
            ProductId = productId;
            NameFull = nameFull;
            Quantity = quantity;
            FreeQuantity = freeQuantity;
        }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int FreeQuantity { get; set; }

        public string NameFull { get; set; }
    }
}
