using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestConfirmReturnDto
    {
        public ServiceRequestConfirmReturnDto(
            int id,
            decimal amount,
            int currencyId,
            string comment,
            RefundRequisitesDto refundRequisites = null)
        {
            Id = id;
            Amount = amount;
            CurrencyId = currencyId;
            Comment = comment;
            RefundRequisites = refundRequisites;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("refund_requisites")]
        public RefundRequisitesDto RefundRequisites { get; set; }
    }
}