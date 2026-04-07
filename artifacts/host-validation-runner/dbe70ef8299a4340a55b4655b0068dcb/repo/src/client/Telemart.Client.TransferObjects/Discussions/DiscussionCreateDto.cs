using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionCreateDto
    {
        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("body")]
        public string Body { get; init; }

        [JsonProperty("format_body")]
        public string FormatBody { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("executor_employee_id")]
        public int ExecutorEmployeeId { get; init; }

        [JsonProperty("co_executor_employee_ids")]
        public int[] CoExecutorEmployeeIds { get; init; }

        [JsonProperty("auditor_employee_ids")]
        public int[] AuditorEmployeeIds { get; init; }

        [JsonProperty("hashtag_ids")]
        public int[] HashtagIds { get; init; }

        [JsonProperty("priority_bitrix")]
        public bool PriorityBitrix { get; init; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; init; }

        [JsonProperty("task_control_bitrix")]
        public bool TaskControlBitrix { get; init; }

        [JsonProperty("deadline")]
        public DateTime? Deadline { get; init; }

        [JsonProperty("use_bitrix_group_from_executor_department")]
        public bool UseBitrixGroupFromExecutorDepartment { get; init; }

        [JsonProperty("entity_documents")]
        public IReadOnlyCollection<DiscussionEntityDocumentDto> EntityDocuments { get; init; }
    }
}