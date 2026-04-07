using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    // TODO: Rewrite to action
    public sealed class QueryPurchaseInvoiceSources : RestClientGatewayRequestBase<List<PurchaseInvoiceSourceDto>>
    {
        public QueryPurchaseInvoiceSources(int contractorId, int orderProductId)
            : base(HttpMethod.Post)
        {
            Body = new PurchaseInvoiceSourcesRequest
            {
                SupplierId = contractorId,
                OrderProductId = orderProductId,
            };

            PathParameters = new object[] { ApiResources.Purchases, "sources", "invoice" };
        }

        public class PurchaseInvoiceSourcesRequest
        {
            [JsonProperty("supplier_id")]
            public int SupplierId { get; set; }

            [JsonProperty("order_product_id")]
            public int OrderProductId { get; set; }
        }
    }
}