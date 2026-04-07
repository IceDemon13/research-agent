using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInCoefDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("coef")]
        public decimal Coef { get; init; }

        [JsonProperty("indicator_value_id")]
        public int IndicatorValueId { get; init; }

        [JsonProperty("indicator_id")]
        public int IndicatorId { get; init; }
    }
}