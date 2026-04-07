using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeContextDto : EmployeeDto
    {
        [JsonProperty("allow_cashboxes")]
        public HashSet<int> AllowCashboxes { get; set; }

        [JsonProperty("allow_categories")]
        public HashSet<int> AllowCategories { get; set; }

        [JsonProperty("allow_warehouses")]
        public HashSet<int> AllowWarehouses { get; set; }

        [JsonProperty("allow_subdivisions")]
        public HashSet<int> AllowSubdivisions { get; set; }

        [JsonIgnore]
        public HashSet<int> AllowedOperations { get; set; }
    }
}