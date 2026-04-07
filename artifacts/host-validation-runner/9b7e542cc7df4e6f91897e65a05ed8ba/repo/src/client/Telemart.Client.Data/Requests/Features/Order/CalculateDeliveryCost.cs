using System.Net.Http;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    // TODO: Rewrite to action
    public sealed class CalculateDeliveryCost : RestClientGatewayRequestBase<CostDto>
    {
        public CalculateDeliveryCost(OrderDeliveryCostDto costDto)
            : base(HttpMethod.Post)
        {
            Body = costDto;

            PathParameters = new object[] { ApiResources.Cost };
        }
    }
}