namespace Telemart.Client.Dictionaries
{
    public class PrintingSettingsInvoiceFormat : DictionaryItem
    {
        public const int A4Id = 1;
        public const int A5Id = 2;
        public const int TapeId = 3;

        private PrintingSettingsInvoiceFormat(int id, string name)
            : base(id, name, true)
        {
        }

        public static PrintingSettingsInvoiceFormat A4 { get; } = new PrintingSettingsInvoiceFormat(A4Id, "А4");

        public static PrintingSettingsInvoiceFormat A5 { get; } = new PrintingSettingsInvoiceFormat(A5Id, "A5");

        public static PrintingSettingsInvoiceFormat Tape { get; } = new PrintingSettingsInvoiceFormat(TapeId, "Лента");
    }
}
