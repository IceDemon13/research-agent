namespace Telemart.Client.Dictionaries
{
    public sealed class BonusType : DictionaryItem
    {
        public const int DragonPointsId = 1;
        public const int TelemartPointsId = 2;
        public const int TradeInPointsId = 3;

        public BonusType(int id, string name, int cashboxId, int? paymentId, string lifeType)
            : base(id, name, true)
        {
            CashboxId = cashboxId;
            PaymentId = paymentId;
            LifeTime = lifeType;
        }

        public int CashboxId { get; }

        public int? PaymentId { get; }

        public string LifeTime { get; }
    }
}