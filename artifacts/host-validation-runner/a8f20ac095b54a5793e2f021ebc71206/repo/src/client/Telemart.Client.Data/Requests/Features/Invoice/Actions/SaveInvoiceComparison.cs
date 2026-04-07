using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class SaveInvoiceComparison : CallEntityActionWithBodyRequestResultBase<InvoiceDto, InvoiceComparisonSaveDto>
    {
        public SaveInvoiceComparison(InvoiceComparisonSaveDto dto)
            : base(dto.InvoiceId, dto, ApiResources.Invoices, "comparison")
        {
        }
    }
}