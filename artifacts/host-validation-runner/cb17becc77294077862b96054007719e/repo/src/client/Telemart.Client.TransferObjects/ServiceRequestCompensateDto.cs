using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestCompensateDto : ServiceRequestConfirmReturnDto
    {
        public ServiceRequestCompensateDto(
            int id,
            decimal amount,
            int currencyId,
            int? changeOnProductId,
            string comment,
            int? cashboxId,
            RefundRequisitesDto requisites,
            int? paymentTypeId)
            : base(id, amount, currencyId, comment)
        {
            ChangeOnProductId = changeOnProductId;
            Requisites = requisites;
            CashboxId = cashboxId;
            PaymentTypeId = paymentTypeId;
        }

        [JsonProperty("change_on_product_id")]
        public int? ChangeOnProductId { get; set; }

        [JsonProperty("cashbox_id")]
        public int? CashboxId { get; set; }

        [JsonProperty("requisites")]
        public RefundRequisitesDto Requisites { get; set; }

        [JsonProperty("payment_type_id")]
        public int? PaymentTypeId { get; set; }
    }
}