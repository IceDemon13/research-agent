namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRepairState : DictionaryItem
    {
        public const int NewId = 1;
        public const int ConfirmedId = 2;
        public const int SentToServiceCenterId = 3;
        public const int InServiceCenterId = 4;
        public const int RepairedId = 5;
        public const int NotRepairedId = 6;
        public const int DefectWasNotConfirmedId = 7;
        public const int DenyOfWarrantyId = 8;
        public const int RemovedFromRegisterId = 9;
        public const int CancelledId = 10;
        public const int WrongSentId = 11;
        public const int NotSentId = 12;

        private ServiceRepairState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRepairState New { get; } = new ServiceRepairState(NewId, "Новый");

        public static ServiceRepairState Confirmed { get; } = new ServiceRepairState(ConfirmedId, "Согласован");

        public static ServiceRepairState SentToServiceCenter { get; } = new ServiceRepairState(SentToServiceCenterId, "Отправлен в СЦ");

        public static ServiceRepairState InServiceCenter { get; } = new ServiceRepairState(InServiceCenterId, "В СЦ");

        public static ServiceRepairState Repaired { get; } = new ServiceRepairState(RepairedId, "Отремонтирован");

        public static ServiceRepairState NotRepaired { get; } = new ServiceRepairState(NotRepairedId, "Не отремонтирован");

        public static ServiceRepairState DefectWasNotConfirmed { get; } = new ServiceRepairState(DefectWasNotConfirmedId, "Дефект не подтвержден");

        public static ServiceRepairState DenyOfWarranty { get; } = new ServiceRepairState(DenyOfWarrantyId, "Отказ в гарантии");

        public static ServiceRepairState RemovedFromRegister { get; } = new ServiceRepairState(RemovedFromRegisterId, "Списан");

        public static ServiceRepairState Cancelled { get; } = new ServiceRepairState(CancelledId, "Отменен");

        public static ServiceRepairState WrongSent { get; } = new ServiceRepairState(WrongSentId, "Не туда отправили");

        public static ServiceRepairState NotSent { get; } = new ServiceRepairState(NotSentId, "Не уехал в СЦ");
    }
}