using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RefundCreateDto : RefundSaveDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}