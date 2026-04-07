using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification;

public sealed record NotificationSubscribeDto
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("employee_id")]
    public int EmployeeId { get; init; }

    [JsonProperty("notification_type_id")]
    public int NotificationTypeId { get; init; }

    [JsonProperty("notification_entity_id")]
    public int? NotificationEntityId { get; init; }

    [JsonProperty("target_ids")]
    public int[] TargetIds { get; init; }

    [JsonProperty("config")]
    public NotificationSubscribeConfigDto Config { get; init; }
}