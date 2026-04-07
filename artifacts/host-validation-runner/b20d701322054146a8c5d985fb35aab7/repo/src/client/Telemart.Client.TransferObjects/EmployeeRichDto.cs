using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeRichDto : EmployeeDto
    {
        [JsonProperty("allow_cashboxes")]
        public IReadOnlyCollection<int> AllowCashboxes { get; set; }

        [JsonProperty("allow_categories")]
        public IReadOnlyCollection<int> AllowCategories { get; set; }

        [JsonProperty("allow_warehouses")]
        public IReadOnlyCollection<int> AllowWarehouses { get; set; }

        [JsonProperty("allow_subdivisions")]
        public IReadOnlyCollection<int> AllowSubdivisions { get; set; }

        [JsonProperty("accounts")]
        public IReadOnlyCollection<EmployeeAccountDto> Accounts { get; set; }

        [JsonProperty("employee_operations")]
        public IReadOnlyCollection<EmployeeOperationDto> EmployeeOperations { get; set; }

        [JsonProperty("role_operations")]
        public IReadOnlyCollection<RoleOperationDto> RoleOperations { get; set; }

        [JsonProperty("generic_accounts")]
        public GenericAccountsDto GenericAccounts { get; init; }
    }
}