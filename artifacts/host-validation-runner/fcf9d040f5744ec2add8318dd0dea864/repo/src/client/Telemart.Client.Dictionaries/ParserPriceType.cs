namespace Telemart.Client.Dictionaries
{
    public class ParserPriceType : DictionaryItemBase
    {
        private ParserPriceType(int id, string name)
            : base(id, name)
        {
        }

        public static ParserPriceType Rrp { get; } = new ParserPriceType(1, "РРЦ");

        public static ParserPriceType Retail { get; } = new ParserPriceType(2, "Ретейл");

        public static ParserPriceType Wholesale { get; } = new ParserPriceType(3, "Опт");

        public static ParserPriceType Configurator { get; } = new ParserPriceType(4, "Конфигуратор");
    }
}