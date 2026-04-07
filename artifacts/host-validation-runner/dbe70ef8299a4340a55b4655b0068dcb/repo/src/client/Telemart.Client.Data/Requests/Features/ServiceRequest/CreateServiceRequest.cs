using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class CreateServiceRequest : CreateEntityResultRequestBase<ServiceRequestDto, ServiceRequestCreateDto>
    {
        public CreateServiceRequest(ServiceRequestCreateDto dto)
            : base(dto, ApiResources.ServiceRequests)
        {
        }
    }
}