using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects.Task
{
    public class TaskDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("task")]
        public string Task { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("documents")]
        public JObject Documents { get; set; }

        [JsonProperty("deadline")]
        public DateTime Deadline { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }
    }
}