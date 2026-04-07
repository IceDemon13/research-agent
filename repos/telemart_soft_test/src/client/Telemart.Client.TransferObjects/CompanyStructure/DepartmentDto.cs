using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.CompanyStructure
{
    public sealed record DepartmentDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("employees")]
        public int[] Employees { get; init; }
    }
}