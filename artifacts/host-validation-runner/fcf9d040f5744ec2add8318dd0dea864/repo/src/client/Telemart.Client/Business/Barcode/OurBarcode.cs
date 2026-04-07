using System.Text.RegularExpressions;

namespace Telemart.Client.Business.Barcode
{
    public sealed class OurBarcode
    {
        private const string Pattern1 = @"^TEL-[1-9]+[0-9]*$";
        private const string Pattern2 = @"^TEL-[1-9]+[0-9]*-[1-9]+[0-9]*$";

        public OurBarcode(string barcodeText)
        {
            BarcodeText = barcodeText;

            IsValid = CheckCode(BarcodeText, out int productId, out int quantity);

            ProductId = productId;
            Quantity = quantity;
        }

        public string BarcodeText { get; }

        public bool IsValid { get; }

        public int ProductId { get; }

        public int Quantity { get; }

        private bool CheckCode(string barcodeText, out int productId, out int quantity)
        {
            productId = -1;
            quantity = 1;

            bool valid = false;

            if (!string.IsNullOrEmpty(barcodeText))
            {
                if (Regex.IsMatch(barcodeText, Pattern1))
                {
                    productId = int.Parse(barcodeText.Substring(4));
                    quantity = 1;
                    valid = true;
                }
                else if (Regex.IsMatch(barcodeText, Pattern2))
                {
                    string[] parts = barcodeText.Split('-');
                    productId = int.Parse(parts[1]);
                    quantity = int.Parse(parts[2]);
                    valid = true;
                }
            }

            return valid;
        }
    }
}