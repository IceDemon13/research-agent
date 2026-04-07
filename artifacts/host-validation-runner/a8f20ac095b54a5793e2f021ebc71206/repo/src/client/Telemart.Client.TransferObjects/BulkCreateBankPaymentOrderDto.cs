using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class BulkCreateBankPaymentOrderDto
    {
        public BulkCreateBankPaymentOrderDto(
            int? orderId,
            DateTime? paidOn,
            decimal? amount,
            bool statementSupport,
            string contractorName,
            string reference,
            string comment,
            decimal? fee,
            decimal? totalAmount,
            int? paymentId)
        {
            OrderId = orderId;
            PaidOn = paidOn;
            Amount = amount;
            StatementSupport = statementSupport;
            ContractorName = contractorName;
            Reference = reference;
            Comment = comment;
            Fee = fee;
            TotalAmount = totalAmount;
            PaymentId = paymentId;
        }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("paid_on")]
        public DateTime? PaidOn { get; set; }

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("statement_support")]
        public bool StatementSupport { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [JsonProperty("ref")]
        public string Reference { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("fee")]
        public decimal? Fee { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("total_amount")]
        public decimal? TotalAmount { get; set; }
    }
}