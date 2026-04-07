using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductCard : QueryEntityRequestBase<ProductCardDto>
    {
        public QueryProductCard(int id)
            : base(ApiResources.Products, id, "card")
        {
        }
    }
}