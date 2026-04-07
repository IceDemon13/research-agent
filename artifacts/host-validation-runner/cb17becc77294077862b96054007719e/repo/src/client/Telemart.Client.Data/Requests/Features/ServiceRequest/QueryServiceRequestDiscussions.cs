using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestDiscussions : QueryEntitiesRequestBase<ServiceRequestDiscussionDto>
    {
        public QueryServiceRequestDiscussions(int serviceRequestId)
            : base(ApiResources.ServiceRequests, serviceRequestId, "discussions")
        {
        }
    }
}