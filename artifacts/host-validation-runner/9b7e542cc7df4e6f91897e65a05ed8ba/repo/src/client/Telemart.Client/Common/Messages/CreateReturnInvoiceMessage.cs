using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public class CreateReturnInvoiceMessage
    {
        public CreateReturnInvoiceMessage(InvoiceDto invoiceDto)
        {
            InvoiceDto = invoiceDto;
        }

        public InvoiceDto InvoiceDto { get; }
    }
}
