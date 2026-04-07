using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CloseServiceRequestDiscussion : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public CloseServiceRequestDiscussion(int serviceRequestId)
            : base(serviceRequestId, ApiResources.ServiceRequests, "close-discussion")
        {
        }
    }
}