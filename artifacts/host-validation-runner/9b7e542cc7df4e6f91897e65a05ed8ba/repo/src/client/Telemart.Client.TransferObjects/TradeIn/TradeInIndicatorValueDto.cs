using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInIndicatorValueDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("indicator_id")]
        public int IndicatorId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("description_ukr")]
        public string DescriptionUkr { get; init; }
    }
}