using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Asterisk
{
    public sealed record AsteriskEmployeeStatusDto
    {
        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("dnd")]
        public string Dnd { get; init; }

        [JsonProperty("presence")]
        public string Presence { get; init; }

        [JsonProperty("max_busy_time")]
        public TimeSpan? MaxBusyTime { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }
    }
}