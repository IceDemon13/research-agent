using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bundle
{
    public record BundleCategoryDto
    {
        [JsonProperty("popular_product_ids")]
        public int[] PopularProductIds { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; init; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("discount_mode_id")]
        public int? DiscountModeId { get; init; }

        [JsonProperty("discount_amount")]
        public int? DiscountAmount { get; init; }
    }
}
