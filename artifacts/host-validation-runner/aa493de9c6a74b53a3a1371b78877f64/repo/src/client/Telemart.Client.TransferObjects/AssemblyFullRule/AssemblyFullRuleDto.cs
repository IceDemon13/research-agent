using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.TransferObjects.AssemblyFullRule
{
    public class AssemblyFullRuleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("master_category_id")]
        public int MasterCategoryId { get; set; }

        [JsonProperty("slave_category_id")]
        public int? SlaveCategoryId { get; set; }

        [JsonProperty("operation_id")]
        public int? OperationId { get; set; }

        [JsonProperty("feature")]
        public FeatureSimpleDto Feature { get; set; }

        [JsonProperty("feature_value")]
        public FeatureValueDto FeatureValue { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}