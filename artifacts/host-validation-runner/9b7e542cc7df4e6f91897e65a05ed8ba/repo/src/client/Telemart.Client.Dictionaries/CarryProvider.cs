namespace Telemart.Client.Dictionaries
{
    public sealed class CarryProvider : DictionaryItem
    {
        public const int NovaPoshtaId = 1;
        public const int MeestExpressId = 2;
        public const int UkrPoshtaId = 3;
        public const int TelemartId = 4;

        public CarryProvider(int id, string name, string nameUkr, string nameEn)
            : base(id, name, true)
        {
            NameUkr = nameUkr;
            NameEn = nameEn;
        }

        public string NameUkr { get; }

        public string NameEn { get; }
    }
}