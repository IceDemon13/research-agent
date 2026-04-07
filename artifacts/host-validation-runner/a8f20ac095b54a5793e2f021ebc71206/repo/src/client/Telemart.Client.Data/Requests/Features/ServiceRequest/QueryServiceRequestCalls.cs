using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestCalls : QueryEntitiesRequestBase<CallDto>
    {
        public QueryServiceRequestCalls(int serviceRequestId)
            : base(ApiResources.ServiceRequests, serviceRequestId, "calls")
        {
        }
    }
}