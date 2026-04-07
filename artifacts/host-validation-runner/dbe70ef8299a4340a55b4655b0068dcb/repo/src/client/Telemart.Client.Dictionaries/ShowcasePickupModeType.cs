namespace Telemart.Client.Dictionaries
{
    public class ShowcasePickupModeType : DictionaryItem
    {
        public const int StandartId = 1;

        public ShowcasePickupModeType(int id, string name, string shortName)
            : base(id, name, true)
        {
            ShortName = shortName;
        }

        public string ShortName { get; }

        public static ShowcasePickupModeType Standart { get; } = new ShowcasePickupModeType(StandartId, "Стандартный", "C");
    }
}