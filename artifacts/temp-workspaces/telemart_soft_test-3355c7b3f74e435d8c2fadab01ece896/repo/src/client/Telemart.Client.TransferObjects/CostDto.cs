using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CostDto
    {
        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("paid")]
        public bool Paid { get; set; }
    }
}