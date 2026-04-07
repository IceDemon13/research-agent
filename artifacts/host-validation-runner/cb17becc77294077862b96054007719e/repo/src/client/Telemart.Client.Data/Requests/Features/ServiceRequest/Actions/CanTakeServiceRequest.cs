using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CanTakeServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public CanTakeServiceRequest(int serviceRequestId)
            : base(serviceRequestId, ApiResources.ServiceRequests, "cantake")
        {
        }
    }
}