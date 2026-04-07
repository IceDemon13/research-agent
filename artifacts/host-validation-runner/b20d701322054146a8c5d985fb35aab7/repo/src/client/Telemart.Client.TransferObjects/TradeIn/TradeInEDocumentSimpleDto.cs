using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public record TradeInEDocumentSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("trade_in_id")]
        public int TradeInId { get; init; }

        [JsonProperty("document_id")]
        public int DocumentId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; }

        [JsonProperty("url")]
        public string Url { get; init; }

        [JsonProperty("accept_telemart")]
        public bool AcceptedTelemart { get; init; }

        [JsonProperty("accept_client")]
        public bool AcceptedClient { get; init; }

        [JsonProperty("send_on")]
        public DateTime? SendOn { get; init; }

        [JsonProperty("send_by")]
        public int? SendBy { get; init; }

        [JsonProperty("key_document")]
        public string KeyDocument { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("accepted_telemart_on")]
        public DateTime? AcceptedTelemartOn { get; init; }
    }
}