using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Task
{
    public class PickupProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_name_ua")]
        public string ProductNameUa { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("reason_ids")]
        public List<int> ReasonIds { get; set; }

        [JsonProperty("return_om_main_warehouse")]
        public bool ReturnOnMainWarehouse { get; set; }

        [JsonProperty("keep_on_pickup")]
        public bool KeepOnPickup { get; set; }
    }
}