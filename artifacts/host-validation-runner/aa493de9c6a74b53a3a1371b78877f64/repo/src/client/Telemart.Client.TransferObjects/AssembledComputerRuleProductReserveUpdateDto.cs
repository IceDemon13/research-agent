using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssembledComputerRuleProductReserveUpdateDto
    {
        public AssembledComputerRuleProductReserveUpdateDto(
            int productId,
            int reserveQuantity,
            int employeeId,
            int warehouseId)
        {
            ProductId = productId;
            ReserveQuantity = reserveQuantity;
            EmployeeId = employeeId;
            WarehouseId = warehouseId;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("reserve_quantity")]
        public int ReserveQuantity { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }
    }
}