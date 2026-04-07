using Telemart.Client.Reports.Product;

namespace Telemart.Client.Reports.AssemblyService
{
    public class AssemblyServiceBarcodeReportData : BarcodeReportData
    {
        public AssemblyServiceBarcodeReportData(string serialNumber, string productName, int productId, int quantity)
            : base(productName, productId, quantity)
        {
            SerialNumber = serialNumber?.Trim();
        }

        public string SerialNumber { get; }
    }
}
