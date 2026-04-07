namespace Telemart.Client.Dictionaries
{
    public class ParserSettingsFileSheetsType : DictionaryItemBase
    {
        public const int OneId = 1;
        public const int AllId = 2;
        public const int SelectivelyId = 3;

        public ParserSettingsFileSheetsType(int id, string name)
            : base(id, name)
        {
        }

        public static ParserSettingsFileSheetsType One { get; } = new ParserSettingsFileSheetsType(OneId, "Один лист");

        public static ParserSettingsFileSheetsType All { get; } = new ParserSettingsFileSheetsType(AllId, "Все листы");

        public static ParserSettingsFileSheetsType Selectively { get; } = new ParserSettingsFileSheetsType(SelectivelyId, "Выборочно");
    }
}