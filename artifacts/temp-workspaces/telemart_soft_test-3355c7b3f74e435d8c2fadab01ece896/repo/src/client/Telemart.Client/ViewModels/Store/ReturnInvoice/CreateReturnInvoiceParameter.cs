using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class CreateReturnInvoiceParameter
    {
        public CreateReturnInvoiceParameter(InvoiceDto invoice)
        {
            Invoice = invoice;
        }

        public InvoiceDto Invoice { get; }
    }
}
