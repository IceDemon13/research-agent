using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Customer
{
    public class CustomerSaveDto
    {
        public CustomerSaveDto(
            int id,
            int? employeeId,
            int contractorId,
            string phone1,
            string phone2,
            string email,
            List<int> hashtagIds,
            int validationTries,
            bool notCountBonuses)
        {
            Id = id;
            EmployeeId = employeeId;
            ContractorId = contractorId;
            Phone1 = phone1;
            Phone2 = phone2;
            HashtagIds = hashtagIds;
            Email = email;
            ValidationTries = validationTries;
            NotCountBonuses = notCountBonuses;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; init; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("phone1")]
        public string Phone1 { get; init; }

        [JsonProperty("phone2")]
        public string Phone2 { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }

        [JsonProperty("hashtag_ids")]
        public List<int> HashtagIds { get; init; }

        [JsonProperty("validation_tries")]
        public int ValidationTries { get; init; }

        [JsonProperty("not_count_bonuses")]
        public bool NotCountBonuses { get; init; }
    }
}