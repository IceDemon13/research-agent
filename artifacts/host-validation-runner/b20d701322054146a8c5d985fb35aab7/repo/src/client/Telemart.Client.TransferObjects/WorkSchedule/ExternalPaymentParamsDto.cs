using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkSchedule
{
    public sealed record ExternalPaymentParamsDto
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("recipient_id")]
        public string RecipientId { get; set; }

        [JsonProperty("credit_contract_number")]
        public string ContractNumber { get; set; }

        [JsonProperty("credit_contract_date")]
        public DateTime? ContractDate { get; set; }

        [JsonProperty("credit_offer_id")]
        public int? CreditOfferId { get; set; }
    }
}