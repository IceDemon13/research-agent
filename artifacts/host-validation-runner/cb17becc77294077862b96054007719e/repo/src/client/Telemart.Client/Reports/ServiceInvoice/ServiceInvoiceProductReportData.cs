namespace Telemart.Client.Reports.ServiceInvoice
{
    public class ServiceInvoiceProductReportData
    {
        public ServiceInvoiceProductReportData(string productName, int serviceRequestId, string serialNumber, string defect)
        {
            ProductName = productName;
            ServiceRequestId = serviceRequestId;
            SerialNumber = serialNumber;
            Defect = defect;
        }

        public string ProductName { get; }

        public int ServiceRequestId { get; }

        public string SerialNumber { get; }

        public string Defect { get; }
    }
}
