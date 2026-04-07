using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class FastCompleteServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, FastCompleteServiceRequestProccesingDto>
    {
        public FastCompleteServiceRequest(FastCompleteServiceRequestProccesingDto dto)
            : base(dto.ServiceRequestId, dto, ApiResources.ServiceRequests, "fast_complete")
        {
        }
    }
}