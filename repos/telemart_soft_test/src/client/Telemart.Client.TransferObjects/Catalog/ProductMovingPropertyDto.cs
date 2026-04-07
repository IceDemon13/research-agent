using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public class ProductMovingPropertyDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("product_value")]
        public object ProductValue { get; set; }

        [JsonProperty("category_to_value")]
        public object CategoryValue { get; set; }
    }
}