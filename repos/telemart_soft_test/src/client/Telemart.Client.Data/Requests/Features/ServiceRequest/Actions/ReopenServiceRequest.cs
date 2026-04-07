using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class ReopenServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public ReopenServiceRequest(int serviceRequestId)
            : base(serviceRequestId, ApiResources.ServiceRequests, "reopen")
        {
        }
    }
}