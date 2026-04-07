using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductPriceKinds : QueryEntitiesRequestBase<ProductPriceKindDto>
    {
        public QueryProductPriceKinds()
            : base($"{ApiResources.Products}/price_kinds")
        {
        }
    }
}