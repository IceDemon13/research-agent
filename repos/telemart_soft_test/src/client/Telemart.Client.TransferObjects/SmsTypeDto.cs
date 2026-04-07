using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SmsTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}