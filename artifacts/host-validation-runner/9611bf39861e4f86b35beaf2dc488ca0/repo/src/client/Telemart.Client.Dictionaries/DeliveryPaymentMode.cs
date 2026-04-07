namespace Telemart.Client.Dictionaries
{
    public sealed class DeliveryPaymentMode : DictionaryItem
    {
        private readonly int intValue;

        private DeliveryPaymentMode(int id, string name, string title, int intValue)
            : base(id, name, true)
        {
            this.intValue = intValue;
            Title = title;
        }

        public static DeliveryPaymentMode We { get; } = new DeliveryPaymentMode(1, "We", "Мы", 1);

        public static DeliveryPaymentMode Client { get; } = new DeliveryPaymentMode(2, "Client", "Клиент", 0);

        public string Title { get; }

        public static DeliveryPaymentMode GetFromBoolean(bool deliveryPaymentMode)
        {
            return deliveryPaymentMode ? We : Client;
        }

        public static DeliveryPaymentMode GetFromInt(int deliveryPaymentMode)
        {
            return GetFromBoolean(deliveryPaymentMode == 1);
        }

        public int ToInt()
        {
            return intValue;
        }
    }
}
