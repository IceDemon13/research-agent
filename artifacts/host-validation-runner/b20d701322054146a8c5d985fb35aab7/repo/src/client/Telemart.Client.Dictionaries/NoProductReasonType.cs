namespace Telemart.Client.Dictionaries
{
    public class NoProductReasonType : DictionaryItem
    {
        public const int ErrorPrice = 1;
        public const int RanOutAtSupplier = 2;
        public const int NotTakenOutStockAfterOrdering = 3;
        public const int LackAvailability = 4;
        public const int NotFoundInWarehouse = 5;
        public const int LostInTransit = 6;
        public const int NoPurchase = 8;

        public NoProductReasonType(int id, string name)
            : base(id, name, true)
        {
        }
    }
}