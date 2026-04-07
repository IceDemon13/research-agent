using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CustomerBonusDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("customer_id")]
        public int CustomerId { get; set; }

        [JsonProperty("bonus_type_id")]
        public int BonusTypeId { get; set; }

        [JsonProperty("bonus_type_name")]
        public string BonusTypeName { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}