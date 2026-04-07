using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDimensionsDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("width")]
        public double? Width { get; set; }

        [JsonProperty("height")]
        public double? Height { get; set; }

        [JsonProperty("depth")]
        public double? Depth { get; set; }

        [JsonProperty("weight")]
        public double? Weight { get; set; }
    }
}