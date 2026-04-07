using System.Linq;

namespace Telemart.Client.Business.Barcode
{
    public sealed class Ean13Barcode
    {
        public Ean13Barcode(string barcode)
        {
            BarcodeText = barcode;
            IsValid = CheckCode(BarcodeText?.Trim());
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        ////private static int CalculateChecksum(string code)
        ////{
        ////    if (code == null)
        ////    {
        ////        throw new ArgumentNullException(nameof(code));
        ////    }

        ////    if (code.Length != 12)
        ////    {
        ////        throw new ArgumentException("Code length should be 12, i.e. excluding the checksum digit");
        ////    }

        ////    int sum = 0;

        ////    for (int i = 0; i < 12; i++)
        ////    {
        ////        int v;

        ////        if (!int.TryParse(code[i].ToString(), out v))
        ////        {
        ////            throw new ArgumentException("Invalid character encountered in specified barcode text.");
        ////        }

        ////        sum += i % 2 == 0
        ////            ? v
        ////            : v * 3;
        ////    }

        ////    int check = 10 - (sum % 10);

        ////    return check % 10;
        ////}

        private static bool CheckCode(string code)
        {
            int dummy;

            if (code == null || code.Length != 13 || code.Any(c => !int.TryParse(c.ToString(), out dummy)))
            {
                return false;
            }

            ////char check = (char)('0' + CalculateChecksum(code.Substring(0, 12)));

            ////return code[12] == check;

            return true;
        }
    }
}