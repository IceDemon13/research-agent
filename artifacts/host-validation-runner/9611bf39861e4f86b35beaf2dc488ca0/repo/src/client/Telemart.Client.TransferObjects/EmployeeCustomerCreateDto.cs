using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record EmployeeCustomerCreateDto
    {
        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("surname")]
        public string Surname { get; init; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; init; }

        [JsonProperty("password")]
        public string Password { get; init; }

        [JsonProperty("create_site_customer_info")]
        public bool CreateSiteCustomerInfo { get; init; }
    }
}