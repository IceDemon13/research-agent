using System.Net.Http;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    // TODO: Переделать метод на action
    public sealed class CalculateLogistics : RestClientGatewayRequestBase<OrderLogisticsResultDto>
    {
        public CalculateLogistics(OrderLogisticsDto dto)
            : base(HttpMethod.Post)
        {
            Body = dto;

            PathParameters = new[] { "logistics" };
        }
    }
}