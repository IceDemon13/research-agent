using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestDocument : QueryEntityRequestBase<ServiceRequestDocumentDto>
    {
        public QueryServiceRequestDocument(int id)
            : base(ApiResources.ServiceRequests, "documents", id)
        {
        }
    }
}