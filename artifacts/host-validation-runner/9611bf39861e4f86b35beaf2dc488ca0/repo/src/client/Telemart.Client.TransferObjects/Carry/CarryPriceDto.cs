using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public class CarryPriceDto
    {
        [JsonProperty("carry_price_id")]
        public int Id { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("weight_from")]
        public decimal? WeightFrom { get; set; }

        [JsonProperty("weight_to")]
        public decimal? WeightTo { get; set; }

        [JsonProperty("cost")]
        public int Cost { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}