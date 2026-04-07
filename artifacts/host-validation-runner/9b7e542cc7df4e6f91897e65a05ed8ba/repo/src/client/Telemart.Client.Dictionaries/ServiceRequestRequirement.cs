namespace Telemart.Client.Dictionaries
{
    public sealed class ServiceRequestRequirement : DictionaryItem
    {
        public const int RepairId = 1;
        public const int ChangeId = 2;
        public const int ReturnMoneyId = 3;
        public const int TradeInId = 4;

        private ServiceRequestRequirement(int id, string name, string nameUkr, string value)
            : base(id, name, true)
        {
            Value = value;
            NameUkr = nameUkr;
        }

        public static ServiceRequestRequirement Repair { get; } = new ServiceRequestRequirement(RepairId, "Ремонт", "Ремонт", "Repair");

        public static ServiceRequestRequirement Change { get; } = new ServiceRequestRequirement(ChangeId, "Обмен", "Обмін", "Change");

        public static ServiceRequestRequirement ReturnMoney { get; } = new ServiceRequestRequirement(ReturnMoneyId, "Возврат ДС", "Повернення гровшових коштів", "ReturnMoney");

        public static ServiceRequestRequirement TradeIn { get; } = new ServiceRequestRequirement(TradeInId, "Trade-In", "Trade-In", "Trade-In");

        public string Value { get; }

        public string NameUkr { get; }
    }
}