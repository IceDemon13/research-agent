namespace Telemart.Client.Dictionaries
{
    public sealed class EventType : DictionaryItem
    {
        public const int UnpackOrderId = 1;
        public const int UnpackAndCancelOrderId = 2;

        private EventType(int id, string name)
            : base(id, name, true)
        {
        }

        public static EventType UnpackOrder { get; } = new EventType(UnpackOrderId, "Распаковать заказ");

        public static EventType UnpackAndCancelOrder { get; } = new EventType(UnpackAndCancelOrderId, "Распаковать и отменить заказ");
    }
}