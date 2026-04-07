namespace Telemart.Client.Dictionaries
{
    public sealed class AssemblyServiceState : DictionaryItem
    {
        private const int WaitingId = 1;
        private const int WarehouseId = 2;
        private const int AssemblingId = 3;
        private const int CompletedId = 4;
        private const int AssembledId = 5;
        private const int TestingId = 6;
        private const int DisassembledId = 7;
        private const int DisassemblingId = 8;

        public AssemblyServiceState(int id, string name)
            : base(id, name, true)
        {
        }

        public static AssemblyServiceState Waiting { get; } = new AssemblyServiceState(WaitingId, "Ожидание");

        public static AssemblyServiceState Warehouse { get; } = new AssemblyServiceState(WarehouseId, "На складе");

        public static AssemblyServiceState Assembling { get; } = new AssemblyServiceState(AssemblingId, "Собирается");

        public static AssemblyServiceState Assembled { get; } = new AssemblyServiceState(AssembledId, "Собрана");

        public static AssemblyServiceState Testing { get; } = new AssemblyServiceState(TestingId, "Тестируется");

        public static AssemblyServiceState Completed { get; } = new AssemblyServiceState(CompletedId, "Завершена");

        public static AssemblyServiceState Disassembled { get; } = new AssemblyServiceState(DisassembledId, "Разобрана");

        public static AssemblyServiceState Disassembling { get; } = new AssemblyServiceState(DisassemblingId, "Разбирается");
    }
}