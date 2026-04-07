namespace Telemart.Client.Dictionaries
{
    public sealed class WarrantyType : DictionaryItem
    {
        public const int FromShopId = 1;
        public const int FromManufactureId = 2;

        public WarrantyType(int id, string name) : base(id, name, true)
        {
        }

        public static WarrantyType FromShop { get; } = new WarrantyType(FromShopId, "От магазина");

        public static WarrantyType FromManufacture { get; } = new WarrantyType(FromManufactureId, "От производителя");
    }
}
