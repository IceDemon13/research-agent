using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects
{
    public class CustomerBonusLogDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("customer_id")]
        public int CustomerId { get; set; }

        [JsonProperty("bonus_type_id")]
        public int BonusTypeId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }

        [JsonProperty("create_on")]
        public DateTime? CreatedOn { get; set; }
    }
}