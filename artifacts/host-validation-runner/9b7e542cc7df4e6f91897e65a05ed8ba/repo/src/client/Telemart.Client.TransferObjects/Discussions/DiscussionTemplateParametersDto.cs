using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionTemplateParametersDto
    {
        [JsonProperty("deadline")]
        public TimeSpan? Deadline { get; init; }

        [JsonProperty("executor_employee_id")]
        public int ExecutorEmployeeId { get; init; }

        [JsonProperty("auditor_employee_ids")]
        public int[] AuditorEmployeeIds { get; init; }

        [JsonProperty("co_executor_employee_ids")]
        public int[] CoExecutorEmployeeIds { get; init; }

        [JsonProperty("hashtag_ids")]
        public int[] HashtagIds { get; init; }

        [JsonProperty("client_logs")]
        public bool ClientLogs { get; init; }

        [JsonProperty("use_group_from_executor_department")]
        public bool UseGroupFromExecutorDepartment { get; init; }

        [JsonProperty("task_control_bitrix")]
        public bool TaskControlBitrix { get; init; }
    }
}