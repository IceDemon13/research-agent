using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeOperationDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("allow")]
        public bool Allow { get; set; }
    }
}