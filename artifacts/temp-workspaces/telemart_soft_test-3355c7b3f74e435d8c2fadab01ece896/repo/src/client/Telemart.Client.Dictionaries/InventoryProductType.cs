namespace Telemart.Client.Dictionaries
{
    public class InventoryProductType : DictionaryItem
    {
        public const int NewId = 1;
        public const int UsedId = 2;
        public const int AllId = 3;
        public const int AllExceptAssembliesId = 4;
        public const int AssembliesId = 5;

        private InventoryProductType(int id, string name)
            : base(id, name, true)
        {
        }

        public static InventoryProductType New { get; } = new InventoryProductType(NewId, "Только новые товары");

        public static InventoryProductType Used { get; } = new InventoryProductType(UsedId, "Только уценка");

        public static InventoryProductType All { get; } = new InventoryProductType(AllId, "Все товары");

        public static InventoryProductType AllExceptAssemblies { get; } = new InventoryProductType(AllExceptAssembliesId, "Все товары (без сборок)");

        public static InventoryProductType Assemblies { get; } = new InventoryProductType(AssembliesId, "Только сборки");
    }
}
