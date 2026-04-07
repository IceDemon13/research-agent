using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryPricesByIds : CallActionWithBodyRequestResultBase<ProductPricesDto, QueryPricesByIdsRequest>
    {
        public QueryPricesByIds(QueryPricesByIdsRequest request)
            : base(request, ApiResources.Prices, "query-by-ids")
        {
        }
    }
}