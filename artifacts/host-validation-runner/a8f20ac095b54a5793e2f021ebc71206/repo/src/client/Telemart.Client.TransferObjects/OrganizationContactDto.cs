using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrganizationContactDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("organization_id")]
        public int OrganizationId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee")]
        public EmployeeSimpleDto Employee { get; set; }

        [JsonProperty("position_id")]
        public int PositionId { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }
    }
}