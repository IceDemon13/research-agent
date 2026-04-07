namespace Telemart.Client.Dictionaries
{
    public class ParserSettingsFileSheetsRecognizeType : DictionaryItemBase
    {
        public const int ByNameId = 1;
        public const int ByNumberId = 2;

        public ParserSettingsFileSheetsRecognizeType(int id, string name)
            : base(id, name)
        {
        }

        public static ParserSettingsFileSheetsRecognizeType ByName { get; } = new ParserSettingsFileSheetsRecognizeType(1, "По названию страницы");

        public static ParserSettingsFileSheetsRecognizeType ByNumber { get; } = new ParserSettingsFileSheetsRecognizeType(2, "По номеру страницы");
    }
}