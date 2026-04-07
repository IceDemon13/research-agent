namespace Telemart.Client.Dictionaries
{
    public sealed class PrintingSettingsWarrantyFormat : DictionaryItemBase
    {
        private const int A5Id = 1;
        private const int TapeId = 2;

        private PrintingSettingsWarrantyFormat(int id, string name)
            : base(id, name)
        {
        }

        public static PrintingSettingsWarrantyFormat A5 { get; } = new PrintingSettingsWarrantyFormat(A5Id, "A5");

        public static PrintingSettingsWarrantyFormat Tape { get; } = new PrintingSettingsWarrantyFormat(TapeId, "Лента");
    }
}