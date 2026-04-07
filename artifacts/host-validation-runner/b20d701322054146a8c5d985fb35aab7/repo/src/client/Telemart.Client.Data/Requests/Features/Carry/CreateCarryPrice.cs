using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class CreateCarryPrice : CreateEntityResultRequestBase<CarryPriceDto, CarryPriceDto>
    {
        public CreateCarryPrice(int carryId, CarryPriceDto dto)
            : base(dto, ApiResources.Carries, carryId.ToString(), ApiResources.CarryPrices, "create")
        {
        }
    }
}