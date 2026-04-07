using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ClaimInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}