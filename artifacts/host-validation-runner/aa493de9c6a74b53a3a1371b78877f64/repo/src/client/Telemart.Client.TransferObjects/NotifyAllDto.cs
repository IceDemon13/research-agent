using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class NotifyAllDto
    {
        [JsonProperty("notification_id")]
        public int? NotificationId { get; init; }

        [JsonProperty("header")]
        public string Header { get; init; }

        [JsonProperty("text")]
        public string Text { get; init; }

        [JsonProperty("notification_format_id")]
        public int NotificationFormatId { get; init; }

        [JsonProperty("notification_image_id")]
        public int NotificationImageId { get; init; }

        [JsonProperty("document_id")]
        public int? DocumentId { get; init; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; init; }

        [JsonProperty("document")]
        public DocumentDto Document { get; init; }
    }
}