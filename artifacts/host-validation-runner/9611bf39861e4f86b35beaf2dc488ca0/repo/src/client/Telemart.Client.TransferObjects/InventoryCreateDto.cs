using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class InventoryCreateDto
    {
        [JsonProperty("category_ids")]
        public List<int> CategoryIds { get; set; }

        [JsonProperty("warehouse_ids")]
        public List<int> WarehouseIds { get; set; }

        [JsonProperty("product_type")]
        public int ProductType { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("transfer_scanned_balances")]
        public bool TransferScannedBalances { get; set; }
    }
}