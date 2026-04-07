using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    public class OurAdditionalServiceBarcode
    {
        private const string Pattern = @"^ADS-[1-9](\d+)?$";

        public OurAdditionalServiceBarcode(string barcodeText)
        {
            BarcodeText = barcodeText;
            AdditionalServiceProductId = CheckCode(BarcodeText);
            IsValid = AdditionalServiceProductId > 0;
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        public int AdditionalServiceProductId { get; }

        private int CheckCode(string barcodeText)
        {
            return !string.IsNullOrEmpty(barcodeText) && Regex.IsMatch(barcodeText, Pattern)
                ? int.Parse(barcodeText[4..])
                : -1;
        }
    }
}