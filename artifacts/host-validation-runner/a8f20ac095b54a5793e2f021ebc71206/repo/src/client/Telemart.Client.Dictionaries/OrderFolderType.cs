namespace Telemart.Client.Dictionaries
{
    public class OrderFolderType : DictionaryItem
    {
        public const int AssemblyServiceId = 1;
        public const int AssembledComputerRuleId = 2;
        public const int DisassemblyServiceId = 3;
        public const int BundleId = 4;

        private OrderFolderType(int id, string name)
            : base(id, name, true)
        {
        }

        public static OrderFolderType AssemblyService { get; } = new OrderFolderType(AssemblyServiceId, "Сборка");

        public static OrderFolderType AssembledComputerRule { get; } = new OrderFolderType(AssembledComputerRuleId, "Конфигурация");

        public static OrderFolderType DisassemblyService { get; } = new OrderFolderType(DisassemblyServiceId, "Разборка");

        public static OrderFolderType Bundle { get; } = new OrderFolderType(BundleId, "Бандл");
    }
}