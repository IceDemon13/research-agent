using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class QueryReturnInvoice : QueryEntityRequestBase<ReturnInvoiceDto>
    {
        public QueryReturnInvoice(int id)
            : base(ApiResources.ReturnInvoices, id)
        {
        }
    }
}