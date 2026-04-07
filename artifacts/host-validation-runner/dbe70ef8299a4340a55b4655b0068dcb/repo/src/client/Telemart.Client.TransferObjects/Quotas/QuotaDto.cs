using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Quotas
{
    public sealed record QuotaDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("value")]
        public int Value { get; init; }

        [JsonProperty("plan_value")]
        public int PlanValue { get; init; }

        [JsonProperty("quota_date")]
        public DateTime QuotaDate { get; init; }

        [JsonProperty("department_id")]
        public int DepartmentId { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}