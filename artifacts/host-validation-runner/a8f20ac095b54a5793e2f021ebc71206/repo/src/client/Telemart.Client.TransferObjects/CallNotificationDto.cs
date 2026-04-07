using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CallNotificationDto
    {
        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("state")]
        public CallNotificationState State { get; init; }

        [JsonProperty("type")]
        public CallNotificationType Type { get; init; }

        [JsonProperty("ivr")]
        public string Ivr { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }
    }
}