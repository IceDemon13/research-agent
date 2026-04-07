using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class UpdateReturnInvoice : UpdateEntityResultRequestBase<ReturnInvoiceDto, ReturnInvoiceSaveDto>
    {
        public UpdateReturnInvoice(ReturnInvoiceSaveDto dto)
            : base(dto, ApiResources.ReturnInvoices, dto.Id)
        {
        }
    }
}
