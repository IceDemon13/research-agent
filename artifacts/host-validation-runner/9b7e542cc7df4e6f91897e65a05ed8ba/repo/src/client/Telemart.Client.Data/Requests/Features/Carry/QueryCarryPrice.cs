using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class QueryCarryPrice : QueryEntityRequestBase<CarryPriceDto>
    {
        public QueryCarryPrice(int carryId, int carryPriceId)
            : base(ApiResources.Carries, carryId, ApiResources.CarryPrices, carryPriceId)
        {
        }
    }
}