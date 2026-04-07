using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record DiscountProductPrefixDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("prefix")]
        public string Prefix { get; init; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; init; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; init; }

        [JsonProperty("trade_in")]
        public bool TradeIn { get; init; }
    }
}