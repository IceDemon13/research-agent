using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.LogisticsMap
{
    public sealed class QueryLogisticsMap : QueryRequestBase<LogisticsMapDto>
    {
        public QueryLogisticsMap(IFilteringItem filteringItem)
            : base(ApiResources.Logistics, "map")
        {
            if (filteringItem != null)
            {
                UrlParameters = filteringItem.BuildParameters();
            }
        }
    }
}