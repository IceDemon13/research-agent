using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public class ParserSearchTemplateFeatureDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}