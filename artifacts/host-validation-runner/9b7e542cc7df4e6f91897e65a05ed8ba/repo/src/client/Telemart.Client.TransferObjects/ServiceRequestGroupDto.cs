using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestGroupDto
    {
        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }
    }
}