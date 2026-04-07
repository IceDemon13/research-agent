using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class ChangeAddressReturnInvoiceParameter
    {
        public ChangeAddressReturnInvoiceParameter(int invoiceId)
        {
            InvoiceId = invoiceId;
        }

        public int InvoiceId { get; }
    }
}