using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public class CreateInvoiceAdditionalCost : CreateEntityResultRequestBase<InvoiceAdditionalCostDto, InvoiceAdditionalCostCreateDto>
    {
        public CreateInvoiceAdditionalCost(InvoiceAdditionalCostCreateDto dto)
            : base(dto, ApiResources.Invoices, dto.InvoiceId.ToString(), ApiResources.AdditionalCosts)
        {
        }
    }
}