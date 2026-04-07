using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AccessoryCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("accessory_id")]
        public int AccessoryId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("accessory_category_features")]
        public AccessoryCategoryFeatureDto[] Features { get; set; }
    }
}