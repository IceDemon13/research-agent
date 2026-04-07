namespace Telemart.Client.Dictionaries
{
    public class ServiceInvoiceProductState : DictionaryItem
    {
        private const int NewId = 1;
        private const int AcceptedId = 2;
        private const int NotAcceptedId = 3;

        public ServiceInvoiceProductState(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceInvoiceProductState New { get; } = new ServiceInvoiceProductState(NewId, "Новый");
        public static ServiceInvoiceProductState Accepted { get; } = new ServiceInvoiceProductState(AcceptedId, "Принят");
        public static ServiceInvoiceProductState NotAccepted { get; } = new ServiceInvoiceProductState(NotAcceptedId, "Не принят");
    }
}
