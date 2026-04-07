using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceMovement;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class CreateServiceMovement : CreateEntityResultRequestBase<ServiceMovementDto, ServiceMovementCreateDto>
    {
        public CreateServiceMovement(ServiceMovementCreateDto dto)
            : base(dto, ApiResources.ServiceMovements)
        {
        }
    }
}
