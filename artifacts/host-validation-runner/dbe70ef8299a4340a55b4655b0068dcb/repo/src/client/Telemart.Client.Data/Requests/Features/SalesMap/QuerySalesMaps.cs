using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.SalesMap
{
    public class QuerySalesMaps : QueryEntitiesRequestBase<SalesMapDto>
    {
        public QuerySalesMaps(IFilteringItem filteringItem)
            : base(filteringItem, ApiResources.SalesMap)
        {
        }
    }
}
