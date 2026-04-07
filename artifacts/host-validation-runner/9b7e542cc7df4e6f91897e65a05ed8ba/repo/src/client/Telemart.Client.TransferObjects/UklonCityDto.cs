using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record UklonCityDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("city_id")]
        public int? CityId { get; init; }
    }
}