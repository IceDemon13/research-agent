using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class UpdateCarryPrice : UpdateEntityResultRequestBase<CarryPriceDto, CarryPriceDto>
    {
        public UpdateCarryPrice(int carryId, int carryPriceId, CarryPriceDto dto)
            : base(dto, ApiResources.Carries, carryId, ApiResources.CarryPrices, carryPriceId)
        {
        }
    }
}