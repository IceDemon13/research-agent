using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class CreateInvoice : CreateEntityResultRequestBase<InvoiceDto, InvoiceCreateDto>
    {
        public CreateInvoice(InvoiceCreateDto dto)
            : base(dto, ApiResources.Invoices)
        {
        }
    }
}
