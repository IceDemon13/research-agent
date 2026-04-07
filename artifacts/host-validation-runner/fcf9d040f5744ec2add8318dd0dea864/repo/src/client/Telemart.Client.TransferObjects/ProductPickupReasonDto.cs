using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductPickupReasonDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}