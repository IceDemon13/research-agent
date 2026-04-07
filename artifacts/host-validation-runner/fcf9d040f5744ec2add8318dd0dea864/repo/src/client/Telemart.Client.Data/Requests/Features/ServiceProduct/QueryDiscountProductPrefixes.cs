using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class QueryDiscountProductPrefixes : QueryEntitiesRequestBase<DiscountProductPrefixDto>
    {
        public QueryDiscountProductPrefixes()
        : base(ApiResources.ServiceProducts, "discount_product_prefix")
        {
        }
    }
}