using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class CreateServiceRequestDiscussion : CreateEntityResultRequestBase<ServiceRequestDiscussionDto, ServiceRequestDiscussionCreateDto>
    {
        public CreateServiceRequestDiscussion(ServiceRequestDiscussionCreateDto dto)
            : base(dto, ApiResources.ServiceRequests, dto.ServiceRequestId, "discussions")
        {
        }
    }
}