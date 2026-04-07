using System.Globalization;

namespace Telemart.Client.Reports.Product
{
    public class BarcodeReportData
    {
        public BarcodeReportData(string productName, int productId, int quantity)
        {
            ProductName = productName;
            ProductId = productId;
            Quantity = quantity;

            BarcodeText = $"TEL-{productId.ToString(CultureInfo.InvariantCulture)}";
            BarcodeExtendedText = $"TEL-{productId.ToString(CultureInfo.InvariantCulture)}-{quantity.ToString(CultureInfo.InvariantCulture)}";
        }

        public string BarcodeExtendedText { get; }

        public string BarcodeText { get; }

        public string ProductName { get; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
