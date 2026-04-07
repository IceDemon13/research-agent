using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderSourceDto
    {
        [JsonProperty("order_source_id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("avail_on")]
        public bool AvailOnClient { get; set; }

        [JsonProperty("can_contact_customer")]
        public bool CanContactCustomer { get; set; }
    }
}