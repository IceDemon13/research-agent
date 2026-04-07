using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public sealed class NpDocumentDto
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("entity_type_id")]
        public int? EntityTypeId { get; init; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; init; }

        [JsonProperty("recipient_phone")]
        public string RecipientPhone { get; init; }

        [JsonProperty("recipient_full_name")]
        public string RecipientFullName { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("receive_date")]
        public DateTime? ReceiveDate { get; init; }
    }
}