using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class CreateServiceCenter : CreateEntityResultRequestBase<ServiceCenterDto, ServiceCenterSaveDto>
    {
        public CreateServiceCenter(ServiceCenterSaveDto dto)
            : base(dto, ApiResources.ServiceCenters)
        {
        }
    }
}
