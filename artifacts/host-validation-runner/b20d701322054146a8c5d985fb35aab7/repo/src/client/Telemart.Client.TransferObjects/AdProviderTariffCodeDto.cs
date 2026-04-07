using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AdProviderTariffCodeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("value")]
        public decimal Value { get; init; }

        [JsonProperty("ad_provider_id")]
        public int AdProviderId { get; init; }
    }
}