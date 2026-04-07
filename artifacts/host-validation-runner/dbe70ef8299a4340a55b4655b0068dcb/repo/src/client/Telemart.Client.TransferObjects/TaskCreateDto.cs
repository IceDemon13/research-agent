using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects
{
    public sealed record TaskCreateDto
    {
        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; init; }

        [JsonProperty("employee_ids")]
        public IReadOnlyCollection<int> EmployeeIds { get; init; }

        [JsonProperty("task")]
        public string Task { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("task_deadline_type")]
        public int TaskDeadlineType { get; init; }

        [JsonProperty("work_schedule_type")]
        public int WorkScheduleType { get; init; }

        [JsonProperty("documents")]
        public JObject Documents { get; init; }
    }
}