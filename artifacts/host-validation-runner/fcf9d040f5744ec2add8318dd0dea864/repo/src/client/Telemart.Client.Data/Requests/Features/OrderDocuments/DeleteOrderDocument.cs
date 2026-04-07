using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.OrderDocuments
{
    public sealed class DeleteOrderDocument : DeleteEntityRequestBase
    {
        public DeleteOrderDocument(int documentId)
            : base(ApiResources.Orders, "documents", documentId)
        {
        }
    }
}