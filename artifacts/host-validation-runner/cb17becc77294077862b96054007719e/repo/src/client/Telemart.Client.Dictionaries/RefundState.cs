namespace Telemart.Client.Dictionaries
{
    public sealed class RefundState : DictionaryItem
    {
        private const int NewId = 1;
        private const int ConfirmedId = 2;
        private const int DoneId = 3;
        private const int CanceledId = 4;

        private RefundState(int id, string name)
            : base(id, name, true)
        {
        }

        public static RefundState New { get; } = new RefundState(NewId, "Новый");

        public static RefundState Confirmed { get; } = new RefundState(ConfirmedId, "Подтвержден");

        public static RefundState Done { get; } = new RefundState(DoneId, "Выполнен");

        public static RefundState Canceled { get; } = new RefundState(CanceledId, "Отменен");
    }
}
