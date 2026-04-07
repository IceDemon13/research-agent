using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification;

public sealed record NotificationTypeDto
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("text")]
    public string Text { get; init; }

    [JsonProperty("header")]
    public string Header { get; init; }

    [JsonProperty("entity_id")]
    public int? EntityId { get; init; }

    [JsonProperty("notification_image_id")]
    public int NotificationImageId { get; init; }

    [JsonProperty("subscribe_operation_id")]
    public int? SubscribeOperationId { get; init; }
}