using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class CreateManyServiceRequest : CreateEntityResultRequestBase<List<ServiceRequestDto>, ServiceRequestCreateManyDto>
    {
        public CreateManyServiceRequest(ServiceRequestCreateManyDto dto)
            : base(dto, $"{ApiResources.ServiceRequests}/actions/create_many")
        {
        }
    }
}