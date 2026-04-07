using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentCategorySettingsDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("category_employee_id")]
        public int CategoryEmployeeId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("filled_in_segments")]
        public bool FilledInSegments { get; set; }
    }
}