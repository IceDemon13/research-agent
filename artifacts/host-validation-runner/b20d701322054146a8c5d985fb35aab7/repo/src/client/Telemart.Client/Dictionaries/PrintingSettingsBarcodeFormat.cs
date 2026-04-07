namespace Telemart.Client.Dictionaries
{
    public class PrintingSettingsBarcodeFormat : DictionaryItem
    {
        private const int Barcode30X20Id = 1;
        private const int Barcode50X40Id = 2;

        private PrintingSettingsBarcodeFormat(int id, string name)
            : base(id, name, true)
        {
        }

        public static PrintingSettingsBarcodeFormat Barcode30X20 { get; } = new PrintingSettingsBarcodeFormat(Barcode30X20Id, "30x20");

        public static PrintingSettingsBarcodeFormat Barcode50X40 { get; } = new PrintingSettingsBarcodeFormat(Barcode50X40Id, "50x40");
    }
}
