using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UklonCarDto
    {
        [JsonProperty("color")]
        public string Color { get; init; }

        [JsonProperty("brand")]
        public string Brand { get; init; }

        [JsonProperty("model")]
        public string Model { get; init; }

        [JsonProperty("license_plate")]
        public string LicensePlate { get; init; }
    }
}