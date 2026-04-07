using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class UpdateServiceRequest : UpdateEntityResultRequestBase<ServiceRequestDto, ServiceRequestSaveDto>
    {
        public UpdateServiceRequest(ServiceRequestSaveDto dto)
            : base(dto, ApiResources.ServiceRequests, dto.Id)
        {
        }
    }
}