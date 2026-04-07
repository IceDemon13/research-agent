using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class AnalyzeInvoice : CallEntityActionWithBodyRequestResultBase<InvoiceAnalyzeResultDto, InvoiceAnalyzeDto>
    {
        public AnalyzeInvoice(int id, InvoiceAnalyzeDto dto)
            : base(id, dto, ApiResources.Invoices, "analyze")
        {
        }
    }
}