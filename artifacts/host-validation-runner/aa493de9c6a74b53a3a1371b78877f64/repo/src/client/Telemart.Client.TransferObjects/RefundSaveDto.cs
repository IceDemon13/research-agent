using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RefundSaveDto
    {
        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("cashbox_id")]
        public int? CashboxId { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("order_payment_id")]
        public int? OrderPaymentId { get; set; }

        [JsonProperty("refund_requisites")]
        public RefundRequisitesDto RefundRequisites { get; set; }
    }
}