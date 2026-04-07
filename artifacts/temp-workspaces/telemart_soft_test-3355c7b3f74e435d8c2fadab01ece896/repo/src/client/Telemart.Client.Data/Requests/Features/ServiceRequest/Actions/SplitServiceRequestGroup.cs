using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class SplitServiceRequestGroup : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public SplitServiceRequestGroup(int serviceRequestId)
        : base(serviceRequestId, ApiResources.ServiceRequests, "split")
        {
        }
    }
}
