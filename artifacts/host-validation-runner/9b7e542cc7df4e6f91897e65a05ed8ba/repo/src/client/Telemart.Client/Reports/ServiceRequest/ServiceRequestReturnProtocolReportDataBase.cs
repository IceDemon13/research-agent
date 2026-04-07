namespace Telemart.Client.Reports.ServiceRequest
{
    public class ServiceRequestReturnProtocolReportDataBase
    {
        public ServiceRequestReturnProtocolReportDataBase(string productName, string serialNumber, int orderId)
        {
            ProductName = productName;
            SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? "____________________________" : serialNumber;
            OrderId = orderId;
        }

        public string ProductName { get; set; }

        public string SerialNumber { get; set; }

        public int OrderId { get; set; }
    }
}
