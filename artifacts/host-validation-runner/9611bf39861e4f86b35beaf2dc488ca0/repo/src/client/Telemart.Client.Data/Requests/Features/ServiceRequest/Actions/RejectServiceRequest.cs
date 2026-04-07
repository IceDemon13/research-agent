using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class RejectServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestRejectDto>
    {
        public RejectServiceRequest(int serviceRequestId, string reason, string alternative, int reasonId)
            : base(
                serviceRequestId,
                new ServiceRequestRejectDto
                {
                    Id = serviceRequestId,
                    Reason = reason,
                    Alternative = alternative,
                    ReasonId = reasonId
                },
                ApiResources.ServiceRequests,
                "reject")
        {
        }
    }
}