using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureValueExDto : FeatureValueDto
    {
        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("multi_language")]
        public bool MultiLanguage { get; set; }
    }
}