namespace Telemart.Client.Dictionaries
{
    public sealed class CashboxProvider : DictionaryItemBase
    {
        public const int CheckboxId = 1;
        
        public CashboxProvider(int id, string name)
            : base(id, name)
        {
        }
        
        public static CashboxProvider Checkbox { get; } = new CashboxProvider(CheckboxId, "Checkbox");

    }
}