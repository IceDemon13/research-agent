using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderBonusSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("bonus_type_id")]
        public int BonusTypeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}