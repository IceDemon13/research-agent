namespace Telemart.Client.Common.Messages
{
    public sealed class ReturnInvoiceEditViewMessage
    {
        public ReturnInvoiceEditViewMessage(int returnInvoiceId)
        {
            ReturnInvoiceId = returnInvoiceId;
        }

        public int ReturnInvoiceId { get; }
    }
}
