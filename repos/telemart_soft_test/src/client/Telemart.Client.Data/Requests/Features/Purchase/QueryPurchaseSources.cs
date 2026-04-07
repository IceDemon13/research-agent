using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    // TODO: Rewrite to action
    public sealed class QueryPurchaseSources : RestClientGatewayRequestBase<ProductSourceDto[]>
    {
        public QueryPurchaseSources(PurchaseSourcesRequest request)
            : base(HttpMethod.Post)
        {
            Body = request;

            PathParameters = new object[] { ApiResources.Purchases, "sources" };
        }

        public class PurchaseSourcesRequest
        {
            [JsonProperty("product_ids")]
            public int[] ProductIds { get; set; }

            [JsonProperty("warehouse_ids")]
            public int[] WarehouseIds { get; set; }

            [JsonProperty("warehouse_type_ids")]
            public int[] WarehouseTypeIds { get; set; }

            [JsonProperty("stock_strategy")]
            [JsonConverter(typeof(StringEnumConverter))]
            public StockStrategy? StockStrategy { get; set; }

            [JsonProperty("include_transits")]
            public bool IncludeTransits { get; set; }

            [JsonProperty("include_invoices")]
            public bool IncludeInvoices { get; set; }
        }
    }
}