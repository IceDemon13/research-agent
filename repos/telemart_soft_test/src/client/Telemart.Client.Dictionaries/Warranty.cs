namespace Telemart.Client.Dictionaries
{
    public class Warranty : DictionaryItem
    {
        public Warranty(
            int id,
            string name,
            string nameUa,
            string nameEn,
            string shortName,
            string shortNameUa,
            string shortNameEn,
            double weight,
            bool tradeInDefault)
            : base(id, name, true)
        {
            NameUa = nameUa;
            NameEn = nameEn;
            ShortName = shortName;
            ShortNameUa = shortNameUa;
            ShortNameEn = shortNameEn;
            Weight = weight;
            TradeInDefault = tradeInDefault;
        }

        public string NameUa { get; }

        public string NameEn { get; }

        public string ShortName { get; }

        public string ShortNameUa { get; }

        public string ShortNameEn { get; }

        public double Weight { get; }

        public bool TradeInDefault { get; }
    }
}