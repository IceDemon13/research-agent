namespace Telemart.Client.Dictionaries
{
    public sealed class ReturnInvoiceState : DictionaryItem
    {
        private const int NewId = 1;
        private const int ReceivedId = 2;
        private const int CanceledId = 3;
        private const int WaitingConfirmId = 4;
        private const int ConfirmedId = 5;

        private ReturnInvoiceState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ReturnInvoiceState New { get; } = new ReturnInvoiceState(NewId, "Новый");

        public static ReturnInvoiceState Sent { get; } = new ReturnInvoiceState(ReceivedId, "Отправлен");

        public static ReturnInvoiceState Canceled { get; } = new ReturnInvoiceState(CanceledId, "Отменен");

        public static ReturnInvoiceState WaitingConfirm { get; } = new ReturnInvoiceState(WaitingConfirmId, "На согласовании");

        public static ReturnInvoiceState Confirmed { get; } = new ReturnInvoiceState(ConfirmedId, "Согласован");
    }
}
