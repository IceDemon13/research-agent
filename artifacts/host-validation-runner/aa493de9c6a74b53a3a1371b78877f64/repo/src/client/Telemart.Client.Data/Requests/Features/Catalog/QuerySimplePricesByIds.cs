using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QuerySimplePricesByIds : CallActionWithBodyRequestResultBase<ProductPricesDto, QueryPricesByIdsRequest>
    {
        public QuerySimplePricesByIds(QueryPricesByIdsRequest request)
            : base(request, ApiResources.Prices, "query-simple-by-ids")
        {
        }
    }
}