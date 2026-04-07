using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductSearchResponseProductDto
    {
        [JsonProperty("ratio")]
        public double Ratio { get; set; }

        [JsonProperty("product")]
        public ProductDto Product { get; set; }
    }
}