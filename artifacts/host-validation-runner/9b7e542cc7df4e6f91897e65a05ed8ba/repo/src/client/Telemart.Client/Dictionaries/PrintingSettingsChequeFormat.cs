namespace Telemart.Client.Dictionaries
{
    public class PrintingSettingsChequeFormat : DictionaryItem
    {
        private const int A4Id = 1;
        private const int CheckTapeId = 2;
        private const int A5Id = 3;

        private PrintingSettingsChequeFormat(int id, string name, int? invoiceFormatId)
            : base(id, name, true)
        {
            InvoiceFormatId = invoiceFormatId;
        }

        public static PrintingSettingsChequeFormat A4 { get; } = new PrintingSettingsChequeFormat(A4Id, "А4", 1);

        public static PrintingSettingsChequeFormat A5 { get; } = new PrintingSettingsChequeFormat(A5Id, "А5", 2);

        public static PrintingSettingsChequeFormat CheckTape { get; } = new PrintingSettingsChequeFormat(CheckTapeId, "Лента", null);

        public int? InvoiceFormatId { get; }
    }
}