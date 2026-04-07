using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public class EmployeeAccountSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}