using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Asterisk
{
    public sealed record AsteriskEmployeeSaveDto
    {
        [JsonProperty("max_busy_time")]
        public TimeSpan? MaxBusyTime { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }
    }
}