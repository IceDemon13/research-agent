using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Quotas
{
    public sealed record QuotaCreateDto
    {
        [JsonProperty("department_id")]
        public int DepartmentId { get; init; }

        [JsonProperty("value")]
        public int Value { get; init; }

        [JsonProperty("plan_value")]
        public int PlanValue { get; init; }
    }
}