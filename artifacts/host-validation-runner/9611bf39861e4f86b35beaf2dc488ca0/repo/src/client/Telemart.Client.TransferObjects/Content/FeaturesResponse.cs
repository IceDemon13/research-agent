using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeaturesResponse
    {
        [JsonProperty("data")]
        public IReadOnlyCollection<ProductContentDto> Data { get; set; }

        [JsonProperty("meta")]
        public FeaturesMetadataDto Meta { get; set; }
    }
}