using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class TakeServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestTakeDto>
    {
        public TakeServiceRequest(int serviceRequestId, ServiceRequestTakeDto dto)
            : base(serviceRequestId, dto, ApiResources.ServiceRequests, "take")
        {
        }
    }
}