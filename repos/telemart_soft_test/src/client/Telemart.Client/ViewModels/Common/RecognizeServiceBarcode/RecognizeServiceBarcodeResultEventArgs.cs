namespace Telemart.Client.ViewModels.Common.RecognizeServiceBarcode
{
    public class RecognizeServiceBarcodeResultEventArgs
    {
        private RecognizeServiceBarcodeResultEventArgs(
            int? serviceRequestId,
            string barcodeText,
            string errorText)
        {
            ServiceRequestId = serviceRequestId;
            BarcodeText = barcodeText;
            ErrorText = errorText;
        }

        public string BarcodeText { get; }

        public int? ServiceRequestId { get; }

        public string ErrorText { get; }

        public bool IsValid => string.IsNullOrWhiteSpace(ErrorText);

        public static RecognizeServiceBarcodeResultEventArgs Error(string barcode, string errorText)
        {
            return new RecognizeServiceBarcodeResultEventArgs(null, barcode, errorText);
        }

        public static RecognizeServiceBarcodeResultEventArgs Found(int serviceRequestId, string barcode)
        {
            return new RecognizeServiceBarcodeResultEventArgs(serviceRequestId, barcode, string.Empty);
        }
    }
}
