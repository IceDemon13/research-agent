namespace Telemart.Client.Dictionaries
{
    public sealed class Language : DictionaryItemBase
    {
        public const int RussianId = 3;
        public const int UkrainianId = 4;
        public const int EnglishId = 5;

        private Language(int id, string name, string shortName) 
            : base(id, name)
        {
            ShortName = shortName;
        }

        public string ShortName { get; }

        public static Language Russian { get; } = new Language(RussianId, "Русский", "RUS");

        public static Language Ukrainian { get; } = new Language(UkrainianId, "Украинский", "UA");
        
        public static Language English { get; } = new Language(EnglishId, "Английский", "EN");
    }
}
