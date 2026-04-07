using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class EmployeePositionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}