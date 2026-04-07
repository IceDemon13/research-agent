using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductColorDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hex")]
        public string Color { get; set; }
    }
}