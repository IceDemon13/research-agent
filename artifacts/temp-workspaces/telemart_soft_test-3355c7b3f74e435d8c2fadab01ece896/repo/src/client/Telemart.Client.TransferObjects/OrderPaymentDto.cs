using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderPaymentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; set; }

        [JsonProperty("sign")]
        public sbyte Sign { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("amount_paid")]
        public decimal AmountPaid { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("completed_on_fiscal_registrar")]
        public bool CompletedOnFiscalRegistrar { get; set; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("refund_state_id")]
        public int? RefundStateId { get; set; }

        [JsonProperty("refund_id")]
        public int? RefundId { get; set; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; set; }

        [JsonProperty("rn")]
        public string Rn { get; set; }

        [JsonProperty("rrn")]
        public string Rrn { get; set; }

        [JsonProperty("check_number")]
        public uint? CheckNumber { get; set; }

        [JsonProperty("terminal_mac_address")]
        public string TerminalMacAddress { get; set; }

        [JsonProperty("fiscal_cashbox_id")]
        public int? FiscalCashboxId { get; init; }

        [JsonProperty("bonus_type_id")]
        public int? BonusTypeId { get; set; }
    }
}