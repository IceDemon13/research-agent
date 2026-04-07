namespace Telemart.Client.Dictionaries
{
    public class FeatureValueValidator : DictionaryItem
    {
        private const int IntId = 1;
        private const int FloatId = 2;

        // private const int DateId = 3;
        private const int RequiredId = 4;
        private const int RegexId = 5;

        public FeatureValueValidator(int id, string name, string regex)
            : base(id, name, true)
        {
            Regex = regex;
        }

        public static FeatureValueValidator Int { get; } = new FeatureValueValidator(IntId, "Целое число", "^[+,-]?[0-9]+$");

        public static FeatureValueValidator Float { get; } = new FeatureValueValidator(FloatId, "Целое или дробное число", @"^[+,-]?([0-9]+[.])?[0-9]+$");

        public static FeatureValueValidator Required { get; } = new FeatureValueValidator(RequiredId, "Заполнено", "^.+$");

        public static FeatureValueValidator RegexValidator { get; } = new FeatureValueValidator(RegexId, "Regex", string.Empty);

        public string Regex { get; }
    }
}
