using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class CreateReturnInvoice : CreateEntityResultRequestBase<ReturnInvoiceDto, ReturnInvoiceCreateDto>
    {
        public CreateReturnInvoice(ReturnInvoiceCreateDto dto)
            : base(dto, ApiResources.ReturnInvoices)
        {
        }
    }
}
