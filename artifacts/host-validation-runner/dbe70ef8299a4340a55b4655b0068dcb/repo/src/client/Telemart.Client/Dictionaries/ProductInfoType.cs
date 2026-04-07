namespace Telemart.Client.Dictionaries
{
    public class ProductInfoType : DictionaryItem
    {
        public const int ComplectId = 1;
        public const int AdvantagesId = 2;

        public ProductInfoType(int id, string name, string addButtonTitle)
            : base(id, name, true)
        {
            AddButtonTitle = addButtonTitle;
        }

        public static ProductInfoType Complect { get; } = new ProductInfoType(ComplectId, "Комплектация", "Комплектацию");

        public static ProductInfoType Advantages { get; } = new ProductInfoType(AdvantagesId, "Преимущества", "Преимущества");

        public string AddButtonTitle { get; }
    }
}
