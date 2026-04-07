using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreatePresaleDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("sn")]
        public string SerialNumber { get; set; }

        [JsonProperty("stated_defect")]
        public string StatedDefect { get; set; }

        [JsonProperty("order_source_id")]
        public int? OrderSourceId { get; set; }
    }
}