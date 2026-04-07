using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public class CarryTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}