using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallDependencyTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}