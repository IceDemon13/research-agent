namespace Telemart.Client.ViewModels.Common.PrintBarcodeParameters
{
    public class PrintBarcodeParametersParameter
    {
        public PrintBarcodeParametersParameter(int quantity, int copies)
        {
            Quantity = quantity;
            Copies = copies;
        }

        public int Quantity { get; }

        public int Copies { get; }
    }
}
