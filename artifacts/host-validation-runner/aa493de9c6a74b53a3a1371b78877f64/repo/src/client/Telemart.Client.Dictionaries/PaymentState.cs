namespace Telemart.Client.Dictionaries
{
    public class PaymentState : DictionaryItem
    {
        public const int CreatedId = 1;
        public const int CancelledId = 2;
        public const int CompletedId = 3;
        public const int ErrorId = 4;
        public const int ConfirmedId = 5;
        public const int PayedId = 6;
        public const int AfterConfirmedId = 8;
        public const int ProcessingId = 9;
        public const int PaymentControlReceivedId = 10;

        private PaymentState(int id, string name)
            : base(id, name, true)
        {
        }

        public static PaymentState Created { get; } = new PaymentState(CreatedId, "Оформляется");

        public static PaymentState Cancelled { get; } = new PaymentState(CancelledId, "Отменен");

        public static PaymentState Completed { get; } = new PaymentState(CompletedId, "Оформлен");

        public static PaymentState Error { get; } = new PaymentState(ErrorId, "Ошибка");

        public static PaymentState Confirmed { get; } = new PaymentState(ConfirmedId, "Одобрен");

        public static PaymentState Payed { get; } = new PaymentState(PayedId, "Оплачен");

        public static PaymentState AfterConfirmed { get; } = new PaymentState(AfterConfirmedId, "Ожидает выполнения");

        public static PaymentState Processing { get; } = new PaymentState(ProcessingId, "В обработке");

        public static PaymentState PaymentControlReceived { get; } = new PaymentState(PaymentControlReceivedId, "Контроль оплаты (списан)");

        public static bool IsActual(int paymentStateId)
        {
            return paymentStateId != ErrorId && paymentStateId != CancelledId;
        }
    }
}