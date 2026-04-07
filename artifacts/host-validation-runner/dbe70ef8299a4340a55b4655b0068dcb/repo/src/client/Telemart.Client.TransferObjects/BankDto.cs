using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class BankDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mfo")]
        public string Mfo { get; set; }
    }
}