using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public class BacklogTaskDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; set; }

        [JsonProperty("author_id")]
        public int AuthorId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("resolution_id")]
        public int? ResolutionId { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("bitrix_id")]
        public int BitrixId { get; set; }

        [JsonProperty("jira_id")]
        public string JiraId { get; set; }

        [JsonProperty("estimate")]
        public int? Estimate { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("favorite_employee_ids")]
        public int[] FavoriteEmployeeIds { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }

        [JsonProperty("quota_id")]
        public int? QuotaId { get; init; }
    }
}