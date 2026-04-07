using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class HashtagCustomerDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("hashtag_id")]
        public int HashtagId { get; set; }

        [JsonProperty("customer_id")]
        public int CustomerId { get; set; }
    }
}