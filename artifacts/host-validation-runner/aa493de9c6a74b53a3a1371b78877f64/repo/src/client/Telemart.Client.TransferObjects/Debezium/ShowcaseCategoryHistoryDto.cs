using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Debezium
{
    public sealed record ShowcaseCategoryHistoryDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("place_name")]
        public string PlaceName { get; set; }

        [JsonProperty("id_product")]
        public int ProductId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("id_warehouse")]
        public int WarehouseId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }
    }
}