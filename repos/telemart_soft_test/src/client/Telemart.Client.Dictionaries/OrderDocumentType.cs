namespace Telemart.Client.Dictionaries
{
    public sealed class OrderDocumentType : DictionaryItemBase
    {
        public const int ChequeId = 1;
        public const int AcceptanceProtocolId = 2;

        public OrderDocumentType(int id, string name)
            : base(id, name)
        {
        }
    }
}