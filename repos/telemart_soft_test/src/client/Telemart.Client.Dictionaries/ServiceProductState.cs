namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceProductState : DictionaryItem
    {
        private const int NewId = 1;
        private const int OnReapirId = 2;
        private const int OnUtilizationId = 3;
        private const int TransferToSupplierId = 4;
        private const int RemovingFromRegisterId = 5;
        private const int RemovedFromRegisterId = 6;
        private const int UtilizedId = 7;
        private const int DiscountedId = 8;
        private const int OnWarehouseId = 9;
        private const int MovementId = 11;

        private ServiceProductState(int id, string name, bool completed)
            : base(id, name, true)
        {
            Completed = completed;
        }

        public static ServiceProductState New { get; } = new ServiceProductState(NewId, "Новый", false);

        public static ServiceProductState OnReapir { get; } = new ServiceProductState(OnReapirId, "В ремонте", false);

        public static ServiceProductState OnUtilization { get; } = new ServiceProductState(OnUtilizationId, "На утилизации", false);

        public static ServiceProductState TransferToSupplier { get; } = new ServiceProductState(TransferToSupplierId, "Передан поставщику", false);

        public static ServiceProductState RemovingFromRegister { get; } = new ServiceProductState(RemovingFromRegisterId, "На списании", false);

        public static ServiceProductState RemovedFromRegister { get; } = new ServiceProductState(RemovedFromRegisterId, "Списан", true);

        public static ServiceProductState Utilized { get; } = new ServiceProductState(UtilizedId, "Утилизирован", true);

        public static ServiceProductState Discounted { get; } = new ServiceProductState(DiscountedId, "Уценен", true);

        public static ServiceProductState OnWarehouse { get; } = new ServiceProductState(OnWarehouseId, "На складе", true);
        
        public static ServiceProductState Movement { get; } = new ServiceProductState(MovementId, "Перемещается", true);
        
        public bool Completed { get; }
    }
}
