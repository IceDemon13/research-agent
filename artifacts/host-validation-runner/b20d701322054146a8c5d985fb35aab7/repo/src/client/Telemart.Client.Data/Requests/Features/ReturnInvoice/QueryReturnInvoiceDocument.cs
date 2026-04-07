using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class QueryReturnInvoiceDocument : QueryEntityRequestBase<ReturnInvoiceDocumentDto>
    {
        public QueryReturnInvoiceDocument(int id)
            : base(ApiResources.ReturnInvoices, "documents", id)
        {
        }
    }
}