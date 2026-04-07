using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestRejectDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("reason_id")]
        public int ReasonId { get; set; }

        [JsonProperty("alternative")]
        public string Alternative { get; set; }
    }
}