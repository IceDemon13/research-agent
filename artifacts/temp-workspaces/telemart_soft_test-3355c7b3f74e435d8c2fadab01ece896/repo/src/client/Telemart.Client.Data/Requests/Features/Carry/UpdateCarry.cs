using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class UpdateCarry : UpdateEntityResultRequestBase<CarryDto, CarrySaveDto>
    {
        public UpdateCarry(CarrySaveDto dto)
            : base(dto, ApiResources.Carries, dto.Id)
        {
        }
    }
}