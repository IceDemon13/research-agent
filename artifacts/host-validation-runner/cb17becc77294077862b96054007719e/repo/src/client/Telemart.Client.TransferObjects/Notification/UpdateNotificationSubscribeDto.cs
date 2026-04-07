using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification
{
    public sealed record UpdateNotificationSubscribeDto
    {
        public UpdateNotificationSubscribeDto(int id, int employeeId, int notificationTypeId, int[] targetIds, NotificationSubscribeConfigDto config)
        {
            EmployeeId = employeeId;
            NotificationTypeId = notificationTypeId;
            TargetIds = targetIds;
            Id = id;
            Config = config;
        }

        [JsonProperty("id")]
        public int Id { get; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; }

        [JsonProperty("notification_type_id")]
        public int NotificationTypeId { get; }

        [JsonProperty("target_ids")]
        public int[] TargetIds { get; }

        [JsonProperty("config")]
        public NotificationSubscribeConfigDto Config { get; }
    }
}