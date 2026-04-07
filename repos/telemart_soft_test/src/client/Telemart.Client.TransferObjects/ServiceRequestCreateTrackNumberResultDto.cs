using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestCreateTrackNumberResultDto
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}