using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductVideoDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("youtube_hash")]
        public string Hash { get; set; }
    }
}