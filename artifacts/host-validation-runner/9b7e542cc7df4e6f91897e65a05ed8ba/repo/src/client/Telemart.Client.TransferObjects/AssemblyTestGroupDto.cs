using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblyTestGroupDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("parent_name")]
        public string ParentName { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("tests")]
        public AssemblyTestDto[] Tests { get; set; }
    }
}