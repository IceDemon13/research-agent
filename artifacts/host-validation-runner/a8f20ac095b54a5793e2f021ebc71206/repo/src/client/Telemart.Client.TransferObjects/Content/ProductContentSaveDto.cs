using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class ProductContentSaveDto
    {
        public ProductContentSaveDto(int id, IReadOnlyCollection<FeatureProductSaveDto> features)
        {
            Id = id;
            Features = features;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("features")]
        public IReadOnlyCollection<FeatureProductSaveDto> Features { get; set; }
    }
}