using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class NoReasonOrderProductDto
    {
        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("quantity")]
        public int? Quentity { get; set; }

        [JsonProperty("source_id")]
        public int? SourceId { get; set; }
    }
}