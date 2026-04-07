using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderReceiveMoneyDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("left_to_pay_uah")]
        public decimal LeftToPayUah { get; set; }
    }
}