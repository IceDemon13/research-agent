using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class InventoryPrintDto
    {
        [JsonProperty("inventory_id")]
        public int InventoryId { get; set; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; set; }

        [JsonProperty("employee_name")]
        public string EmployeeName { get; set; }

        [JsonProperty("products")]
        public List<InventoryPrintProductDto> Products { get; set; }
    }
}