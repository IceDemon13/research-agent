using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductImageDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("cover")]
        public bool Cover { get; set; }
    }
}