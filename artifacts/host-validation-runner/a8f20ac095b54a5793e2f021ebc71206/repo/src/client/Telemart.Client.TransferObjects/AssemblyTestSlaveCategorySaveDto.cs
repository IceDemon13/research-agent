using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssemblyTestSlaveCategorySaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}