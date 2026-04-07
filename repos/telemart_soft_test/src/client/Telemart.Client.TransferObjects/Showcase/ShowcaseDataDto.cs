using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record ShowcaseDataDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; init; }

        [JsonProperty("warehouse_quantity_free")]
        public int WarehouseQuantityFree { get; init; }

        [JsonProperty("warehouse_category_plan")]
        public int WarehouseCategoryPlan { get; init; }

        [JsonProperty("sales_quantity")]
        public int SalesQuantity { get; init; }

        [JsonProperty("can_buy")]
        public bool CanBuy { get; init; }
    }
}