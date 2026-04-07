namespace Telemart.Client.Dictionaries
{
    public sealed class CashboxType : DictionaryItem
    {
        private const int RetailId = 1;
        private const int CardId = 2;
        private const int PaymentAccountId = 3;
        private const int VirtualId = 4;
        private const int EmployeeCashboxId = 5;
        private const int FiscalRegistrarId = 6;
        private const int StrongboxId = 7;

        private CashboxType(int id, string name)
            : base(id, name, true)
        {
        }

        public static CashboxType Retail { get; } = new CashboxType(RetailId, "Розница");

        public static CashboxType Card { get; } = new CashboxType(CardId, "Карта");

        public static CashboxType PaymentAccount { get; } = new CashboxType(PaymentAccountId, "Р/с");

        public static CashboxType Virtual { get; } = new CashboxType(VirtualId, "Виртуальная");

        public static CashboxType Employee { get; } = new CashboxType(EmployeeCashboxId, "Касса сотрудника");

        public static CashboxType FiscalRegistrar { get; } = new CashboxType(FiscalRegistrarId, "РРО");

        public static CashboxType Strongbox { get; } = new CashboxType(StrongboxId, "Сейф");
    }
}
