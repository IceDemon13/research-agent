using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class DeleteServiceRequestDocument : DeleteEntityRequestBase
    {
        public DeleteServiceRequestDocument(int documentId)
            : base(ApiResources.ServiceRequests, "documents", documentId.ToString())
        {
        }
    }
}