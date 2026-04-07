using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("rule_id")]
        public int RuleId { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

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

        [JsonProperty("use_any_products")]
        public bool UseAnyProducts { get; init; }

        [JsonProperty("compare_method")]
        public string CompareMethod { get; init; }

        [JsonProperty("product_type_ids")]
        public int[] ProductTypeIds { get; init; }
    }
}