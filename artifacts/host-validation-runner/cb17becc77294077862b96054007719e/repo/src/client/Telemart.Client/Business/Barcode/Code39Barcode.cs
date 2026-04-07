using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    internal class Code39Barcode
    {
        private const string Code39Pattern = @"^[A-Z0-9\s\*\-\$\.\+\/%]+$";

        public Code39Barcode(string barcodeText)
        {
            BarcodeText = barcodeText;
            IsValid = CheckCode(BarcodeText);
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        private bool CheckCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            return Regex.IsMatch(code, Code39Pattern);
        }
    }
}