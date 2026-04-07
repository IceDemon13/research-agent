using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestDocuments : QueryEntitiesRequestBase<ServiceRequestDocumentSimpleDto>
    {
        public QueryServiceRequestDocuments(int serviceRequestId)
            : base(ApiResources.ServiceRequests, serviceRequestId, "documents")
        {
        }
    }
}