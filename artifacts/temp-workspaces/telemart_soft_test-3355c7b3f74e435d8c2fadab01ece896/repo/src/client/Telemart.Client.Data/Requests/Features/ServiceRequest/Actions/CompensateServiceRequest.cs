using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class CompensateServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, ServiceRequestCompensateDto>
    {
        public CompensateServiceRequest(int serviceRequestId, ServiceRequestCompensateDto dto)
        : base(serviceRequestId, dto, ApiResources.ServiceRequests, "compensate")
        {
        }
    }
}