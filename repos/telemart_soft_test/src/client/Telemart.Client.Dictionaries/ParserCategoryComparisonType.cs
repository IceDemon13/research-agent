namespace Telemart.Client.Dictionaries
{
    public sealed class ParserCategoryComparisonType : DictionaryItem
    {
        public const int OrdinalId = 1;
        public const int RegexId = 2;
        public const int LikeId = 3;

        public string Description { get; }

        private ParserCategoryComparisonType(int id, string name, string description)
            : base(id, name, true)
        {
            Description = description;
        }

        public static ParserCategoryComparisonType Ordinal { get; } = new ParserCategoryComparisonType(OrdinalId, "Full", "Полное совпадение");

        public static ParserCategoryComparisonType Like { get; } = new ParserCategoryComparisonType(LikeId, "Like", "Частичное совпадение");

        public static ParserCategoryComparisonType Regex { get; } = new ParserCategoryComparisonType(RegexId, "Regex", "Регулярное выражение");
    }
}
