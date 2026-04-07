namespace Telemart.Client.Dictionaries
{
    public sealed class ProductCatalogActivityState : DictionaryItem
    {
        private ProductCatalogActivityState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ProductCatalogActivityState Activated { get; } = new ProductCatalogActivityState(1, "Активен");

        public static ProductCatalogActivityState WaitingForActivation { get; } = new ProductCatalogActivityState(2, "Ожидает активации");

        public static ProductCatalogActivityState VisibleInB2B { get; } = new ProductCatalogActivityState(3, "Виден в B2B");

        public static ProductCatalogActivityState Hidden { get; } = new ProductCatalogActivityState(4, "Скрыт");
    }
}
