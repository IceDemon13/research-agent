using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class UpdateServiceCenter : UpdateEntityResultRequestBase<ServiceCenterDto, ServiceCenterSaveDto>
    {
        public UpdateServiceCenter(ServiceCenterSaveDto dto)
            : base(dto, ApiResources.ServiceCenters, dto.Id)
        {
        }
    }
}
