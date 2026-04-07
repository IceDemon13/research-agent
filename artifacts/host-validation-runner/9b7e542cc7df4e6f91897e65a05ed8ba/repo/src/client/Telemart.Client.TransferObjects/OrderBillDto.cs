using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderBillDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("bill_1c_id")]
        public string Bill1cId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("organization_account_id")]
        public int OrganizationAccountId { get; set; }

        [JsonProperty("organization_id")]
        public int OrganizationId { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("expire_date")]
        public DateTime? ExpireDate { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

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