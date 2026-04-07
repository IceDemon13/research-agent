namespace Telemart.Client.Dictionaries
{
    public sealed class CallDependencyType : DictionaryItem
    {
        public const int OrderId = 1;
        public const int ServiceRequestId = 2;
        public const int ComplaintId = 3;
        public const int OrderProductId = 4;
        public const int CallId = 5;

        public CallDependencyType(int id, string name)
            : base(id, name, true)
        {
        }
    }
}