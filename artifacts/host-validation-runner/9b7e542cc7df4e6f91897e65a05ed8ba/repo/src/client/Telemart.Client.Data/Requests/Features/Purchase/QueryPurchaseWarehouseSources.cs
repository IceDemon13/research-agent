using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    // TODO: Rewrite to action
    public sealed class QueryPurchaseWarehouseSources : RestClientGatewayRequestBase<List<PurchaseWarehouseSourceDto>>
    {
        public QueryPurchaseWarehouseSources(int productId, int? targetWarehouseId, bool includeReserve)
            : base(HttpMethod.Post)
        {
            Body = new PurchaseWarehouseSourcesRequest(
                targetWarehouseId is > 0
                    ? targetWarehouseId.Value
                    : null,
                productId,
                includeReserve);

            PathParameters = new object[] { ApiResources.Purchases, "sources", "warehouse" };
        }

        public class PurchaseWarehouseSourcesRequest
        {
            public PurchaseWarehouseSourcesRequest(int? targetWarehouseId, int productId, bool includeReserve)
            {
                TargetWarehouseId = targetWarehouseId;
                ProductId = productId;
                IncludeReserve = includeReserve;
            }

            [JsonProperty("target_warehouse_id")]
            public int? TargetWarehouseId { get; set; }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }

            [JsonProperty("include_reserve")]
            public bool IncludeReserve { get; set; }
        }
    }
}