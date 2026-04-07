using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public class WorkAccountDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}