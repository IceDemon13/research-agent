namespace Telemart.Client.Dictionaries
{
    public class PhoneHistoryType : DictionaryItem
    {
        public const int OrderId = 1;
        public const int ServiceRequestId = 2;
        public const int CallId = 3;
        public const int ComplaintId = 4;
        public const int TradeInId = 5;

        public PhoneHistoryType(int id, string name)
            : base(id, name, true)
        {
        }

        public static PhoneHistoryType Order { get; } = new PhoneHistoryType(OrderId, "Заказ");

        public static PhoneHistoryType ServiceRequest { get; } = new PhoneHistoryType(ServiceRequestId, "Сервисная заявка");

        public static PhoneHistoryType Call { get; } = new PhoneHistoryType(CallId, "Звонок");

        public static PhoneHistoryType Complaint { get; } = new PhoneHistoryType(ComplaintId, "Жалоба");

        public static PhoneHistoryType TradeIn { get; } = new PhoneHistoryType(TradeInId, "Trade-In");
    }
}
