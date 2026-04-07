using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class DeleteCarryPrice : DeleteEntityRequestBase
    {
        public DeleteCarryPrice(int carryId, int carryPriceId)
            : base(ApiResources.Carries, carryId.ToString(), ApiResources.CarryPrices, carryPriceId.ToString())
        {
        }
    }
}