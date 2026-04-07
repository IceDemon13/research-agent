using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UklonDriverDto
    {
        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("rating")]
        public float Rating { get; init; }

        [JsonProperty("completed_orders")]
        public int CompletedOrders { get; init; }

        [JsonProperty("disability_type")]
        public UklonDriverDisabilityType DisabilityType { get; init; }
    }
}