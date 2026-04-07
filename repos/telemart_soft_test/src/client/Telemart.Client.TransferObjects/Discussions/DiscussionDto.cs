using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Discussions
{
    public sealed record DiscussionDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; }

        [JsonProperty("body")]
        public string Body { get; init; }

        [JsonProperty("type_id")]
        public string TypeId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; init; }

        [JsonProperty("deadline")]
        public DateTime? Deadline { get; init; }

        [JsonProperty("bitrix_id")]
        public string BitrixId { get; init; }

        [JsonProperty("executor_employee_id")]
        public int ExecutorEmployeeId { get; init; }

        [JsonProperty("priority_bitrix")]
        public bool PriorityBitrix { get; init; }

        [JsonProperty("co_executor_employee_ids")]
        public int[] CoExecutorEmployeeIds { get; init; }

        [JsonProperty("auditor_employee_ids")]
        public int[] AuditorEmployeeIds { get; init; }

        [JsonProperty("hashtag_ids")]
        public int[] HashtagIds { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("entity_documents")]
        public IReadOnlyCollection<DiscussionEntityDocumentDto> EntityDocuments { get; init; }
    }
}