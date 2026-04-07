using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    // TODO: Rewrite to action
    public sealed class QueryProductTags : RestClientGatewayRequestBase<List<ProductTagDto>>
    {
        public QueryProductTags(
            int priceId,
            IReadOnlyCollection<int> productIds,
            int? warehouseId,
            DateTime? priceUpdatedAfterDateTime,
            int? tagFormatId,
            bool? logTagSelection)
            : base(HttpMethod.Post)
        {
            PathParameters = new object[] { ApiResources.Products, "tags" };

            Body = new GetProductTagsRequest(priceId, productIds, priceUpdatedAfterDateTime, warehouseId, tagFormatId, logTagSelection);
        }

        private class GetProductTagsRequest
        {
            public GetProductTagsRequest(
                int priceId,
                IReadOnlyCollection<int> productIds,
                DateTime? priceUpdatedAfterDateTime,
                int? warehouseId,
                int? tagFormatId,
                bool? logTagSelection)
            {
                PriceId = priceId;
                ProductIds = productIds;
                PriceUpdatedAfterDateTime = priceUpdatedAfterDateTime;
                WarehouseId = warehouseId;
                TagFormatId = tagFormatId;
                LogTagSelection = logTagSelection;
            }

            [JsonProperty("price_id")]
            public int PriceId { get; set; }

            [JsonProperty("product_ids")]
            public IReadOnlyCollection<int> ProductIds { get; set; }

            [JsonProperty("date_time")]
            public DateTime? PriceUpdatedAfterDateTime { get; set; }

            [JsonProperty("warehouse_id")]
            public int? WarehouseId { get; set; }

            [JsonProperty("tag_format_id")]
            public int? TagFormatId { get; set; }

            [JsonProperty("log_tag_selection")]
            public bool? LogTagSelection { get; set; }
        }
    }
}