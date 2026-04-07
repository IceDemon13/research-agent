using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Telemart.Client.TransferObjects
{
    public class ProductSourceDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ProductSourceType Type { get; set; }

        [JsonProperty("avail_on")]
        public DateTime? AvailOn { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("reserved_by_order")]
        public int ReservedByOrder { get; set; }

        [JsonProperty("reserved_by_return_invoice")]
        public int ReservedByReturnInvoice { get; set; }

        [JsonProperty("reserved_by_assembly_complectation")]
        public int ReservedByAssemblyComplectation { get; set; }

        [JsonProperty("reserved_by_showcase")]
        public int ReservedByShowcase { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("raw")]
        public JObject RawSource { get; set; }

        [JsonProperty("leftovers_reserve_quantity")]
        public int? LeftoversReserveQuantity { get; set; }
    }
}