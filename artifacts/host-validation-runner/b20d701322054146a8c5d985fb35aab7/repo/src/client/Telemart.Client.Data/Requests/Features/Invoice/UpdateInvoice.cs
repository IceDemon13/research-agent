using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class UpdateInvoice : UpdateEntityResultRequestBase<InvoiceDto, InvoiceSaveDto>
    {
        public UpdateInvoice(InvoiceSaveDto dto)
            : base(dto, ApiResources.Invoices, dto.Id)
        {
        }
    }
}
