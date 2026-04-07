namespace Telemart.Client.Dictionaries
{
    public class BankPaymentState : DictionaryItem
    {
        public const int NewId = 1;
        public const int ProcessedId = 2;
        public const int IgnoreId = 3;

        private BankPaymentState(int id, string name)
            : base(id, name, true)
        {
        }

        public static BankPaymentState New { get; } = new BankPaymentState(NewId, "Новый");

        public static BankPaymentState Processed { get; } = new BankPaymentState(ProcessedId, "Проведен");

        public static BankPaymentState Ignore { get; } = new BankPaymentState(IgnoreId, "Не учитывать");
    }
}
