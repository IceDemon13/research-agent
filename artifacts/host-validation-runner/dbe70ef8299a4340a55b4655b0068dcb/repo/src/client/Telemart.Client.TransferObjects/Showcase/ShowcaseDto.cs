using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public class ShowcaseDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_parent_category_id")]
        public int ProductParentCategoryId { get; set; }

        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }
    }
}