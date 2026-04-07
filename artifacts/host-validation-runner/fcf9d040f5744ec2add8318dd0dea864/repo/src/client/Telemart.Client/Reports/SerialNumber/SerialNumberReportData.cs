namespace Telemart.Client.Reports.SerialNumber
{
    public sealed class SerialNumberReportData
    {
        public SerialNumberReportData(int serviceRequestId, string barcodeText)
        {
            ServiceRequestId = serviceRequestId;
            BarcodeText = barcodeText;
        }

        public string BarcodeText { get; }

        public int ServiceRequestId { get; }
    }
}
