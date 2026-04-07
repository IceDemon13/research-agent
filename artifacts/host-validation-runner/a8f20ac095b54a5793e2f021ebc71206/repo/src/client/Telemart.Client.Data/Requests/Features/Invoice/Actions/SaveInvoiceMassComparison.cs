using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class SaveInvoiceMassComparison : CallActionWithBodyRequestResultBase<object, InvoiceMassComparisonSaveDto>
    {
        public SaveInvoiceMassComparison(InvoiceMassComparisonSaveDto dto)
            : base(dto, ApiResources.Invoices, "mass_comparison")
        {
        }
    }
}