using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeUpdateDto
    {
        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("phone1")]
        public string Phone1 { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("skype")]
        public string Skype { get; set; }

        [JsonProperty("telegram")]
        public string Telegram { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("department_id")]
        public int DepartmentId { get; set; }

        [JsonProperty("contractor_template_id")]
        public int? ContractorTemplateId { get; set; }

        [JsonProperty("card_key")]
        public string CardKey { get; set; }

        [JsonProperty("client_access_denied")]
        public bool ClientAccessDenied { get; set; }

        [JsonProperty("roles")]
        public List<string> Roles { get; set; }

        [JsonProperty("allow_cashboxes")]
        public List<int> AllowCashboxes { get; set; }

        [JsonProperty("allow_categories")]
        public List<int> AllowCategories { get; set; }

        [JsonProperty("allow_warehouses")]
        public List<int> AllowWarehouses { get; set; }

        [JsonProperty("allow_subdivisions")]
        public List<int> AllowSubdivisions { get; set; }

        [JsonProperty("accounts")]
        public List<EmployeeAccountSaveDto> Accounts { get; set; }

        [JsonProperty("employee_operations")]
        public List<EmployeeOperationSaveDto> EmployeeOperations { get; set; }
    }
}