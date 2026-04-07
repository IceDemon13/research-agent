namespace Telemart.Client.Dictionaries
{
    public sealed class OrderSourceType : DictionaryItem
    {
        public const int Site = 1;
        public const int StoreId = 2;
        public const int InnerOrder = 7;
        public const int Monomarket = 13;

        public OrderSourceType(int id, string name, int position, bool availOnClient, bool cannContactCustomer)
            : base(id, name, true)
        {
            Position = position;
            AvailOnClient = availOnClient;
            CanContactCustomer = cannContactCustomer;
        }

        public int Position { get; }

        public bool AvailOnClient { get; }

        public bool CanContactCustomer { get; }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(OrderSourceType other)
        {
            return Position.CompareTo(other?.Position ?? -1);
        }

        public override int CompareTo(DictionaryItemBase obj)
        {
            return CompareTo(obj as OrderSourceType);
        }
    }
}