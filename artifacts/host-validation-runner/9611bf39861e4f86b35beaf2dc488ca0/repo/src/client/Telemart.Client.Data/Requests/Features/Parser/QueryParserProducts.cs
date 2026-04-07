using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser
{
    public sealed class QueryParserProducts : QueryEntitiesRequestBase<ProductComparsionDto>
    {
        private static readonly string ProductsResource = $"{ApiResources.Parser}/products";

        public QueryParserProducts()
            : base(ProductsResource)
        {
        }
    }
}