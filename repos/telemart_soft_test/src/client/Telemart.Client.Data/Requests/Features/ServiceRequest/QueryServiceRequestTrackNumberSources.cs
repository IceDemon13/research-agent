using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class QueryServiceRequestTrackNumberSources : QueryEntitiesRequestBase<CreateServiceRequestTrackNumberSourceDto>
    {
        public QueryServiceRequestTrackNumberSources(int serviceRequestId)
            : base(ApiResources.ServiceRequests, serviceRequestId, "tnsources")
        {
        }
    }
}