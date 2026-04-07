using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InventoryQuantitiesSaveDto
    {
        [JsonProperty("id_inventory")]
        public int InventoryId { get; set; }

        [JsonProperty("transfer_scanned_balances")]
        public bool TransferScannedBalances { get; init; }

        [JsonProperty("inventory_products_ids")]
        public List<InventoryProductQuantityRealDto> InventoryProductsQuantities { get; set; }
    }
}