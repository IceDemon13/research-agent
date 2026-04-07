using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.CompanyStructure
{
    public sealed record DepartmentCreateDto
    {
        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("parent_id")]
        public int? DepartmentParentId { get; init; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; init; }
    }
}