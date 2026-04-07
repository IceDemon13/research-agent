using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestRepairs : QueryEntitiesRequestBase<ServiceRepairDto>
    {
        public QueryServiceRequestRepairs(int serviceRequestId)
            : base(ApiResources.ServiceRequests, serviceRequestId, "repairs")
        {
        }
    }
}