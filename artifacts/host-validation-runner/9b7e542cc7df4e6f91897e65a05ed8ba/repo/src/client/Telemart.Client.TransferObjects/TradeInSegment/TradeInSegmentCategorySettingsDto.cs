using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentCategorySettingsDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("category_employee_id")]
        public int CategoryEmployeeId { get; init; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; init; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; init; }

        [JsonProperty("filled_in_segments")]
        public bool FilledInSegments { get; init; }
    }
}
