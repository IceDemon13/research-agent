using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblyTestDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }

        [JsonProperty("main_category_id")]
        public int MainCategoryId { get; set; }

        [JsonProperty("main_feature_ids")]
        public int[] MainFeatureIds { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        [JsonProperty("avail_on_web")]
        public bool AvailOnWeb { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("slave_categories")]
        public AssemblyTestSlaveCategoryDto[] SlaveCategories { get; set; }
    }
}