using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeDto : TrackableDtoBase<int>
    {
        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("department_id")]
        public int DepartmentId { get; set; }

        [JsonProperty("contractor_template_id")]
        public int? ContractorTemplateId { get; set; }

        [JsonProperty("phone1")]
        public string Phone1 { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("skype")]
        public string Skype { get; set; }

        [JsonProperty("telegram")]
        public string Telegram { get; set; }

        [JsonProperty("card_key")]
        public string CardKey { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("position_id")]
        public int PositionId { get; init; }

        [JsonProperty("roles")]
        public IReadOnlyCollection<string> Roles { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("client_access_denied")]
        public bool ClientAccessDenied { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("fired_on")]
        public DateTime? FiredOn { get; set; }

        [JsonProperty("fired_by")]
        public int? FiredBy { get; set; }
    }
}