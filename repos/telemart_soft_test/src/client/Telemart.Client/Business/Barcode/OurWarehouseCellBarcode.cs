using System.Globalization;
using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    public class OurWarehouseCellBarcode
    {
        private const string Pattern = @"^CELL-[1-9](\d+)?$";

        public OurWarehouseCellBarcode(string barcodeText)
        {
            BarcodeText = barcodeText;
            CellId = CheckCode(BarcodeText);
            IsValid = CellId > 0;
        }

        public OurWarehouseCellBarcode(int cellId)
        {
            BarcodeText = $"CELL-{cellId.ToString(CultureInfo.InvariantCulture)}";
            CellId = cellId;
            IsValid = true;
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        public int CellId { get; }

        private int CheckCode(string barcodeText)
        {
            return !string.IsNullOrEmpty(barcodeText) && Regex.IsMatch(barcodeText, Pattern)
                ? int.Parse(barcodeText.Substring(5))
                : -1;
        }
    }
}
