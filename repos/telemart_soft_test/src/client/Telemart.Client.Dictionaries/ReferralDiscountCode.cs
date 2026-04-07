namespace Telemart.Client.Dictionaries
{
    public sealed class ReferralDiscountCode : DictionaryItemBase
    {
        public ReferralDiscountCode(int id, string name, decimal value)
            : base(id, name)
        {
            Value = value;
        }
        
        public decimal Value { get; }
    }
}