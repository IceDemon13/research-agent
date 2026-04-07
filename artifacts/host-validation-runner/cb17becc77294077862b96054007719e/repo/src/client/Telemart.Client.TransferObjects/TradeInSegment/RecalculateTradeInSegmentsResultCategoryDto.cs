using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class RecalculateTradeInSegmentsResultCategoryDto
    {
        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; init; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; init; }

        [JsonProperty("calculated_products_quantity")]
        public int CalculatedProductsQuantity { get; init; }

        [JsonProperty("products_quantity_with_not_filled_features_from_parent_category_segments")]
        public int ProductsQuantityWithNotFilledFeaturesFromParentCategorySegments { get; init; }

        [JsonProperty("total_category_products_quantity")]
        public int TotalCategoryProductsQuantity { get; init; }

        [JsonProperty("trade_in_segment_id")]
        public int TradeInSegmentId { get; init; }

        [JsonProperty("trade_in_segment_name")]
        public string TradeInSegmentName { get; init; }
    }
}
