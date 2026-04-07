using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public class ParserSearchTemplateDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("features")]
        public ParserSearchTemplateFeatureDto[] Features { get; set; }
    }
}