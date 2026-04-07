using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class ResetServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public ResetServiceRequest(int serviceRequestId)
            : base(serviceRequestId, ApiResources.ServiceRequests, "reset")
        {
        }
    }
}