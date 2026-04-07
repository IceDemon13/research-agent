using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssembledComputerRuleReserveProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("sales_quantity")]
        public int SalesQuantity { get; set; }

        [JsonProperty("min_leftover")]
        public int? MinLeftover { get; set; }

        [JsonProperty("warehouse_quantity")]
        public int? WarehouseQuantity { get; set; }

        [JsonProperty("warehouse_quantity_free")]
        public int? WarehouseQuantityFree { get; set; }

        [JsonProperty("assembled_computer_rule_quantity")]
        public int AssembledComputerRuleQuantity { get; set; }

        [JsonProperty("reserve_quantity")]
        public int ReserveQuantity { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("sales_quantity_inside_assembled_computer_rule")]
        public int SalesQuantityInsideAssembledComputerRule { get; set; }

        [JsonProperty("reserved_by_assembled_computer_rule_quantity")]
        public int ReservedByAssembledComputerRuleQuantity { get; set; }
    }
}