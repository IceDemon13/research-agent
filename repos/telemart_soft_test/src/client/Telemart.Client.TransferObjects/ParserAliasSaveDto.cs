using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserAliasSaveDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }
    }
}