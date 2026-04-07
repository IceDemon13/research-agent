using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyFullRule
{
    public class AssemblyFullRuleSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("master_category_id")]
        public int MasterCategoryId { get; set; }

        [JsonProperty("slave_category_id")]
        public int? SlaveCategoryId { get; set; }

        [JsonProperty("operation_id")]
        public int? OperationId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}