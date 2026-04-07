using System.Collections.Generic;
using System.Net.Http;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    // TODO: Rewrite to action
    public sealed class QueryProductFeatures : RestClientGatewayRequestBase<List<ProductFeatureGroupsDto>>
    {
        public QueryProductFeatures(IReadOnlyCollection<int> productIds)
            : base(HttpMethod.Post)
        {
            Body = productIds;

            PathParameters = new object[] { ApiResources.Products, "features" };
        }
    }
}