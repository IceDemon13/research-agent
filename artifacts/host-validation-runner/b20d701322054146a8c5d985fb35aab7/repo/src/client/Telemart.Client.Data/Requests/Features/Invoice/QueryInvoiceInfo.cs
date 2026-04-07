using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoiceInfo : QueryEntityRequestBase<InvoiceEditingInfoDto>
    {
        public QueryInvoiceInfo(int invoiceId)
            : base("invoices", invoiceId, "edit")
        {
        }
    }
}