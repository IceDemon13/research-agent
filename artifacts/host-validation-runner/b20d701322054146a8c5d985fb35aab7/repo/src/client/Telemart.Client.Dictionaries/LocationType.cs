namespace Telemart.Client.Dictionaries
{
    public class LocationType : DictionaryItem
    {
        public const int ShopId = 1;
        public const int ProductionId = 2;

        public LocationType(int id, string name)
            : base(id, name, true)
        {
        }
    }
}