namespace Telemart.Client.Dictionaries
{
    public sealed class ParserSettingsType : DictionaryItemBase
    {
        private ParserSettingsType(int id, string name)
            : base(id, name)
        {
        }

        public static ParserSettingsType FileAuto { get; } = new ParserSettingsType(1, "Файл (авто)");

        public static ParserSettingsType SiteAuto { get; } = new ParserSettingsType(2, "Сайт (авто)");

        public static ParserSettingsType Excel { get; } = new ParserSettingsType(3, "Excel");
    }
}