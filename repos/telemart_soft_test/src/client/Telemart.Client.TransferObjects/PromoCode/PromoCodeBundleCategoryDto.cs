using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public record PromoCodeBundleCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; init; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; init; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; init; }

        [JsonProperty("feature_value_name")]
        public string FeatureValueName { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("compare_method")]
        public string CompareMethod { get; init; }

        [JsonProperty("discount_mode_id")]
        public int? DiscountModeId { get; init; }

        [JsonProperty("discount_amount")]
        public int? DiscountAmount { get; set; }
    }
}