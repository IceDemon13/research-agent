using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record CreateTradeInDocumentDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("ext")]
        public string Ext { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("trade_in_id")]
        public int TradeInId { get; init; }
    }
}