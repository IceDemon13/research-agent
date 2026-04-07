using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class QueryDiscountProducts : QueryEntitiesRequestBase<ProductCardDto>
    {
        public QueryDiscountProducts(int productId, int serviceProductTypeId)
            : base("products", productId, "discounts", serviceProductTypeId)
        {
        }
    }
}