using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification;

public sealed record NotificationDto
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("employee_id")]
    public int EmployeeId { get; init; }

    [JsonProperty("entity_id")]
    public int? EntityId { get; init; }

    [JsonProperty("document_id")]
    public int DocumentId { get; init; }

    [JsonProperty("notification_type_id")]
    public int NotificationTypeId { get; init; }

    [JsonProperty("text")]
    public string Text { get; init; }

    [JsonProperty("target_id")]
    public int TargetId { get; init; }

    [JsonProperty("received")]
    public bool Received { get; init; }

    [JsonProperty("read")]
    public bool Read { get; init; }

    [JsonProperty("created_on")]
    public DateTime CreatedOn { get; init; }

    [JsonProperty("read_on")]
    public DateTime? ReadOn { get; init; }

    [JsonProperty("received_on")]
    public DateTime? ReceivedOn { get; init; }
}