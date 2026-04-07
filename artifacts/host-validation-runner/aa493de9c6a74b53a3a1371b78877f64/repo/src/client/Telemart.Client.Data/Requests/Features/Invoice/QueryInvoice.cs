using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoice : QueryEntityRequestBase<InvoiceDto>
    {
        public QueryInvoice(int id)
            : base(ApiResources.Invoices, id)
        {
        }
    }
}