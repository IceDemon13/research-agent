using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeatureFullDto : FeatureSaveDto
    {
        [JsonProperty("group")]
        public FeatureGroupSimpleDto Group { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("icon_id")]
        public int? IconId { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
    }
}