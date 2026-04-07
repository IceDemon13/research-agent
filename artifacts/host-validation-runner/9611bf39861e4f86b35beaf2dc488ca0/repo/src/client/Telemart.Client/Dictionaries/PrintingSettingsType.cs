namespace Telemart.Client.Dictionaries
{
    public class PrintingSettingsType : DictionaryItem
    {
        private const int MainId = 1;
        private const int ChequeId = 2;
        private const int WarrantyCardId = 3;
        private const int StickerId = 4;
        private const int Barcode30X20Id = 5;
        private const int SerialNumberId = 6;
        private const int Barcode50X40Id = 7;

        private PrintingSettingsType(int id, string name, int position)
            : base(id, name, true)
        {
            Position = position;
        }

        public static PrintingSettingsType Main { get; } = new PrintingSettingsType(MainId, "Основной", 10);

        public static PrintingSettingsType Cheque { get; } = new PrintingSettingsType(ChequeId, "Расходная накладная (Лента)", 20);

        public static PrintingSettingsType WarrantyCard { get; } = new PrintingSettingsType(WarrantyCardId, "Гарантийние талоны", 30);

        public static PrintingSettingsType Sticker { get; } = new PrintingSettingsType(StickerId, "ТТН", 40);

        public static PrintingSettingsType Barcode30X20 { get; } = new PrintingSettingsType(Barcode30X20Id, "Наш ШК 30x20", 50);

        public static PrintingSettingsType Barcode50X40 { get; } = new PrintingSettingsType(Barcode50X40Id, "Наш ШК 50x40", 50);

        public static PrintingSettingsType SerialNumber { get; } = new PrintingSettingsType(SerialNumberId, "Наш SN", 60);

        public int Position { get; }
    }
}
