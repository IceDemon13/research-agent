using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class QueryReturnInvoiceDocuments : QueryEntitiesRequestBase<ReturnInvoiceDocumentSimpleDto>
    {
        public QueryReturnInvoiceDocuments(int returnInvoiceId)
            : base($"{ApiResources.ReturnInvoices}/{returnInvoiceId}/{ApiResources.Documents}")
        {
        }
    }
}
