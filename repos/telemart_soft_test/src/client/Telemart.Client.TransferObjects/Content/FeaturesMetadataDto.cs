using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeaturesMetadataDto
    {
        [JsonProperty("groups")]
        public IReadOnlyCollection<FeatureGroupDto> Groups { get; set; }

        [JsonProperty("feature_values")]
        public IReadOnlyCollection<FeatureValueExDto> FeatureValues { get; set; }
    }
}