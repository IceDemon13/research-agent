using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class DeleteReturnInvoiceDocument : DeleteEntityRequestBase
    {
        public DeleteReturnInvoiceDocument(int returnInvoiceId, int documentId)
           : base(ApiResources.ReturnInvoices, returnInvoiceId.ToString(), "documents", documentId.ToString())
        {
        }
    }
}