using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SubdivisionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_retail")]
        public bool IsRetail { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("organization")]
        public OrganizationDto Organization { get; set; }
    }
}