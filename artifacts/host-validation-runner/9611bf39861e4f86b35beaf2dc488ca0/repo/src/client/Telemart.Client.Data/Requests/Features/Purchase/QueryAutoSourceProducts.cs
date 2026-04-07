using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.Purchase
{
    public sealed class QueryAutoSourceProducts : CallActionWithBodyRequestBase<IReadOnlyCollection<ProductSourceResultDto>, QueryAutoSourceProducts.ProductSourcesRequest>
    {
        public QueryAutoSourceProducts(ProductSourcesRequest request)
            : base(request, ApiResources.Purchases, "auto_source_products")
        {
        }

        public sealed record ProductSourcesRequest
        {
            public ProductSourcesRequest(
                int subdivisionId,
                int carryId,
                int paymentId,
                int warehouseId,
                int cityId,
                IReadOnlyCollection<int> warehouseIds,
                IReadOnlyCollection<ProductSourcesProductDto> products)
            {
                SubdivisionId = subdivisionId;
                CarryId = carryId;
                PaymentId = paymentId;
                WarehouseId = warehouseId;
                WarehouseIds = warehouseIds;
                CityId = cityId;
                Products = products;
            }

            [JsonProperty("subdivision_id")]
            public int SubdivisionId { get; }

            [JsonProperty("carry_id")]
            public int CarryId { get; }

            [JsonProperty("payment_id")]
            public int PaymentId { get; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; }

            [JsonProperty("city_id")]
            public int CityId { get; }

            [JsonProperty("warehouse_ids")]
            public IReadOnlyCollection<int> WarehouseIds { get; }

            [JsonProperty("products")]
            private IReadOnlyCollection<ProductSourcesProductDto> Products { get; }
        }
    }
}