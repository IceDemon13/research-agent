using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CanDiagnoseServiceRequest : CallEntityActionRequestResultBase<ServiceRequestDto>
    {
        public CanDiagnoseServiceRequest(int serviceRequestId)
            : base(serviceRequestId, ApiResources.ServiceRequests, "candiagnose")
        {
        }
    }
}