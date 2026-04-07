using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeatureDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("separator")]
        public string Separator { get; set; }

        [JsonProperty("mask")]
        public string Mask { get; set; }

        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("multi_language")]
        public bool MultiLanguage { get; set; }

        [JsonProperty("manual_input")]
        public bool ManualInput { get; set; }
    }
}