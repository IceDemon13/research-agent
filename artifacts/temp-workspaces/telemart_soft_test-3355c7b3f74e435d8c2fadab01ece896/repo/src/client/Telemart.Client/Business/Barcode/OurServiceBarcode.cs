using System.Globalization;
using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    public sealed class OurServiceBarcode
    {
        private const string Pattern = @"^SR-[1-9]\d{3,5}$";

        public OurServiceBarcode(string barcodeText)
        {
            BarcodeText = barcodeText;
            ServiceRequestId = CheckCode(BarcodeText);
            IsValid = ServiceRequestId > 0;
        }

        public OurServiceBarcode(int serviceRequestId)
        {
            BarcodeText = $"SR-{serviceRequestId.ToString(CultureInfo.InvariantCulture)}";
            ServiceRequestId = serviceRequestId;
            IsValid = true;
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        public int ServiceRequestId { get; }

        private int CheckCode(string barcodeText)
        {
            return !string.IsNullOrEmpty(barcodeText) && Regex.IsMatch(barcodeText, Pattern)
                ? int.Parse(barcodeText.Substring(3))
                : -1;
        }
    }
}