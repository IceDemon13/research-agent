using System.Net.Http;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.LogisticsAnalitics;

namespace Telemart.Client.Data.Requests.Features.Logistics
{
    public class QueryLogisticsAnalitics : RestClientGatewayRequestBase<LogisticsAnaliticsDto>
    {
        public QueryLogisticsAnalitics(IFilteringItem filter)
            : base(HttpMethod.Get)
        {
            UrlParameters = filter?.BuildParameters();
            PathParameters = new[] { ApiResources.Logistics, "analitics" };
        }
    }
}