using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class InventoryDto
    {
        [JsonProperty("id_inventory")]
        public int Id { get; set; }

        [JsonProperty("id_warehouse")]
        public int WarehouseId { get; set; }

        [JsonProperty("id_employee_lock")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("created_by_id")]
        public int CreatedByEmployeeId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("inventoried_on")]
        public DateTime? InventoriedOn { get; set; }

        [JsonProperty("group_ref")]
        public Guid? GroupRef { get; set; }

        [JsonProperty("inventory_products")]
        public List<InventoryProductDto> InventoryProducts { get; set; }

        [JsonProperty("group_inventories")]
        public List<InventoryDto> GroupInventories { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }

        [JsonProperty("transfer_scanned_balances")]
        public bool TransferScannedBalances { get; set; }
    }
}