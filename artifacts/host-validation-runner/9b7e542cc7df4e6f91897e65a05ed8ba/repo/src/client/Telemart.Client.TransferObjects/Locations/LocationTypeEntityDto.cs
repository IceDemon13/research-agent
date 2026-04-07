using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Locations
{
    public sealed record LocationTypeEntityDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("name_en")]
        public string NameEn { get; init; }
    }
}