using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    // TODO: Rewrite to action
    public sealed class QueryPurchaseMovementSources : RestClientGatewayRequestBase<List<PurchaseMovementSourceDto>>
    {
        public QueryPurchaseMovementSources(int productId, int orderId)
            : base(HttpMethod.Post)
        {
            Body = new PurchaseMovementSourcesRequest
            {
                ProductId = productId,
                OrderId = orderId
            };

            PathParameters = new object[] { ApiResources.Purchases, "sources", "movement" };
        }

        public class PurchaseMovementSourcesRequest
        {
            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }
        }
    }
}