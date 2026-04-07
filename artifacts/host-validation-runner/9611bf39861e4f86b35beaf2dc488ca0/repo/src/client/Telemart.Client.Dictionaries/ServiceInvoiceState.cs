namespace Telemart.Client.Dictionaries
{
    public class ServiceInvoiceState : DictionaryItem
    {
        public const int NewId = 1;
        public const int ClosedId = 2;
        public const int SentId = 3;
        public const int ReceivedId = 4;

        private ServiceInvoiceState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceInvoiceState New { get; } = new ServiceInvoiceState(NewId, "Новая");

        public static ServiceInvoiceState Closed { get; } = new ServiceInvoiceState(ClosedId, "Закрыта");

        public static ServiceInvoiceState Sent { get; } = new ServiceInvoiceState(SentId, "Отправлена");

        public static ServiceInvoiceState Received { get; } = new ServiceInvoiceState(ReceivedId, "Принята");
    }
}
