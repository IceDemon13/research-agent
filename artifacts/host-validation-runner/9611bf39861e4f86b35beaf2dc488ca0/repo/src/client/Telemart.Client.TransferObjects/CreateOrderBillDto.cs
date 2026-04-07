using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CreateOrderBillDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("organization_account_id")]
        public int OrganizationAccountId { get; set; }

        [JsonProperty("expire_date")]
        public DateTime? ExpireDate { get; set; }
    }
}