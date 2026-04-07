using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Customer
{
    public class CustomerDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; init; }

        [JsonProperty("city_id")]
        public int? CityId { get; init; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("not_count_bonuses")]
        public bool NotCountBonuses { get; init; }

        [JsonProperty("fio")]
        public string Fio { get; init; }

        [JsonProperty("last_name")]
        public string LastName { get; init; }

        [JsonProperty("first_name")]
        public string FirstName { get; init; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; init; }

        [JsonProperty("birthday")]
        public DateTime? Birthday { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }

        [JsonProperty("phone1")]
        public string Phone1 { get; init; }

        [JsonProperty("phone2")]
        public string Phone2 { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("validated")]
        public bool Validated { get; init; }

        [JsonProperty("validation_tries")]
        public int ValidationTries { get; init; }

        [JsonProperty("plus_hashtag_ids")]
        public List<int> PlusHashtagIds { get; init; }

        [JsonProperty("minus_hashtag_ids")]
        public List<int> MinusHashtagIds { get; init; }

        [JsonProperty("bonuses")]
        public List<CustomerBonusDto> Bonuses { get; init; }

        [JsonProperty("assemblies")]
        public List<CustomerAssemblyDto> Assemblies { get; init; }
    }
}