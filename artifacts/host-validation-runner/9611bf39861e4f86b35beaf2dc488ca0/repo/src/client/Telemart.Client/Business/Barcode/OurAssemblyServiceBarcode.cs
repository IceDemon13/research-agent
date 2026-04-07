using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    public class OurAssemblyServiceBarcode
    {
        private const string Pattern = @"^GR-[1-9](\d+)?$";

        public OurAssemblyServiceBarcode(string barcodeText)
        {
            BarcodeText = barcodeText;
            AssemblyServiceId = CheckCode(BarcodeText);
            IsValid = AssemblyServiceId > 0;
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        public int AssemblyServiceId { get; }

        private int CheckCode(string barcodeText)
        {
            return !string.IsNullOrEmpty(barcodeText) && Regex.IsMatch(barcodeText, Pattern)
                ? int.Parse(barcodeText[3..])
                : -1;
        }
    }
}