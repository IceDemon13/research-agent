using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductAttributes : QueryEntityRequestBase<ProductAttributesDto>
    {
        public QueryProductAttributes(int productId)
            : base(ApiResources.Products, productId, "attributes")
        {
        }
    }
}