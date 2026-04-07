using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderCreditDataDto
    {
        public OrderCreditDataDto(
            string contractNumber,
            DateTime? contractDate,
            decimal contractAmount,
            int creditOfferId)
        {
            ContractNumber = contractNumber;
            ContractDate = contractDate;
            ContractAmount = contractAmount;
            CreditOfferId = creditOfferId;
        }

        public OrderCreditDataDto()
        {
        }

        [JsonProperty("contract_number")]
        public string ContractNumber { get; set; }

        [JsonProperty("contract_date")]
        public DateTime? ContractDate { get; set; }

        [JsonProperty("contract_amount")]
        public decimal ContractAmount { get; set; }

        [JsonProperty("credit_offer_id")]
        public int CreditOfferId { get; set; }
    }
}