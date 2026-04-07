using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects
{
    public class ContractorSimpleDto : TrackableDtoBase<int>
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}