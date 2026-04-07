using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record UklonFareDto
    {
        [JsonProperty("minimum_price")]
        public decimal? MinimumPrice { get; init; }

        [JsonProperty("maximum_price")]
        public decimal? MaximumPrice { get; init; }

        [JsonProperty("recommended_price")]
        public decimal? RecommendedPrice { get; init; }

        [JsonProperty("drive_time_seconds")]
        public int DriveTimeSeconds { get; init; }
    }
}