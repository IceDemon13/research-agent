using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInDocumentEditDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }
    }
}