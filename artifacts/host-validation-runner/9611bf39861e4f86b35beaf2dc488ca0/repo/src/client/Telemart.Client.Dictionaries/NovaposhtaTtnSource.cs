namespace Telemart.Client.Dictionaries
{
    public sealed class NovaposhtaTtnSource : DictionaryItem
    {
        private const int OrderId = 1;
        private const int MovementId = 2;
        private const int InvoiceId = 3;
        private const int PaymentId = 4;
        private const int ServiceId = 5;
        private const int ReturnId = 6;
        private const int OtherId = 7;
        private const int DocumentId = 8;

        private NovaposhtaTtnSource(int id, string name)
            : base(id, name, true)
        {
        }

        public static NovaposhtaTtnSource Order { get; } = new NovaposhtaTtnSource(OrderId, "Заказ");

        public static NovaposhtaTtnSource Movement { get; } = new NovaposhtaTtnSource(MovementId, "Перемещение");

        public static NovaposhtaTtnSource Invoice { get; } = new NovaposhtaTtnSource(InvoiceId, "Накладная");

        public static NovaposhtaTtnSource Payment { get; } = new NovaposhtaTtnSource(PaymentId, "Наложка");

        public static NovaposhtaTtnSource Service { get; } = new NovaposhtaTtnSource(ServiceId, "Сервис");

        public static NovaposhtaTtnSource Return { get; } = new NovaposhtaTtnSource(ReturnId, "Возврат");

        public static NovaposhtaTtnSource Other { get; } = new NovaposhtaTtnSource(OtherId, "Прочее");

        public static NovaposhtaTtnSource Document { get; } = new NovaposhtaTtnSource(DocumentId, "Документ");
    }
}
