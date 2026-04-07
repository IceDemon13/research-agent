using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class ChangeServiceRequestRequirement : UpdateEntityResultRequestBase<ServiceRequestDto, ServiceRequestChangeRequirementDto>
    {
        public ChangeServiceRequestRequirement(ServiceRequestChangeRequirementDto dto)
            : base(dto, ApiResources.ServiceRequests, dto.Id, "requirement")
        {
        }
    }
}