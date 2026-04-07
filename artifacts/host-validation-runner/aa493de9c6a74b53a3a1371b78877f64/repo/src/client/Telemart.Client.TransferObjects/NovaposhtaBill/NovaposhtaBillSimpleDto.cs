using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaBill
{
    public class NovaposhtaBillSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contractor_ref")]
        public string ContractorRef { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("invoiced_on")]
        public DateTime? InvoicedOn { get; set; }

        [JsonProperty("payed_on")]
        public DateTime? PayedOn { get; set; }

        [JsonProperty("payed_by")]
        public int? PayedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}