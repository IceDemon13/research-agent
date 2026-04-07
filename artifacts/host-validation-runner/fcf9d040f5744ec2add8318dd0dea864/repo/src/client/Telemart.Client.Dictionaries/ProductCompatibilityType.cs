namespace Telemart.Client.Dictionaries
{
    public class ProductCompatibilityType : DictionaryItem
    {
        private const int PCId = 1;
        private const int AccessoryId = 2;

        public ProductCompatibilityType(int id, string name, string shortName)
            : base(id, name, true)
        {
            ShortName = shortName;
        }

        public string ShortName { get; }

        public static ProductCompatibilityType PC { get; } = new ProductCompatibilityType(PCId, "Сборка ПК", "ПК");

        public static ProductCompatibilityType Accessory { get; } = new ProductCompatibilityType(AccessoryId, "Аксессуары", "А");
    }
}
