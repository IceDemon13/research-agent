using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public class UpdateInvoiceAdditionalCost : UpdateEntityResultRequestBase<InvoiceAdditionalCostDto, InvoiceAdditionalCostSaveDto>
    {
        public UpdateInvoiceAdditionalCost(InvoiceAdditionalCostSaveDto dto)
            : base(dto, $"{ApiResources.Invoices}/{ApiResources.AdditionalCosts}", dto.Id)
        {
        }
    }
}