namespace Telemart.Client.Dictionaries
{
    public class TaxRate : DictionaryItem
    {
        private const int ZeroPercentId = 1;
        private const int SevenPercentId = 2;
        private const int TwentyPercentId = 3;
        private const int NoPercentId = 4;

        private TaxRate(int id, string name, int value, int? value1C)
            : base(id, name, true)
        {
            Value = value;
            Value1C = value1C;
        }

        public static TaxRate ZeroPercent { get; } = new TaxRate(ZeroPercentId, "0%", 0, 0);

        public static TaxRate SevenPercent { get; } = new TaxRate(SevenPercentId, "7%", 7, 7);

        public static TaxRate TwentyPercent { get; } = new TaxRate(TwentyPercentId, "20%", 20, 20);

        public static TaxRate NoPercent { get; } = new TaxRate(NoPercentId, "Без НДС", 0, null);

        public int Value { get; }

        public int? Value1C { get; }
    }
}
