using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.Filter
{
    public class FilterDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("link_rewrite")]
        public string LinkRewrite { get; set; }
    }
}