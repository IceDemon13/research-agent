using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public class EmployeeOperationSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("allow")]
        public bool Allow { get; set; }
    }
}