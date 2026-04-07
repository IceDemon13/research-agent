using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class ProductContentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("ym_id")]
        public string YandexMarketId { get; set; }

        [JsonProperty("features")]
        public IReadOnlyCollection<FeatureProductDto> Features { get; set; }
    }
}