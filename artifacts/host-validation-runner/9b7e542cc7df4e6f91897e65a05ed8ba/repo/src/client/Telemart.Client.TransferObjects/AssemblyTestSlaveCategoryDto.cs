using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblyTestSlaveCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("test_id")]
        public int TestId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}