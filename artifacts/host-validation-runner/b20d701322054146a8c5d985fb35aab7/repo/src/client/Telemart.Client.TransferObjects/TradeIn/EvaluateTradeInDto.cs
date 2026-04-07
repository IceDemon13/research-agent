using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record EvaluateTradeInDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("max_price")]
        public decimal MaxPrice { get; init; }

        [JsonProperty("notify_client")]
        public bool NotifyClient { get; init; }
    }
}