using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public record TradeInDocumentDto : TradeInDocumentSimpleDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; init; }
    }
}