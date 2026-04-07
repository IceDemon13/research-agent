using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record AutoShowcaseDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; init; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; init; }

        [JsonProperty("capacity")]
        public int Capacity { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("category_name")]
        public string CategoryName { get; init; }

        [JsonProperty("category_name_ukr")]
        public string CategoryNameUkr { get; init; }

        [JsonProperty("category_name_en")]
        public string CategoryNameEn { get; init; }

        [JsonProperty("segment_id")]
        public int? SegmentId { get; init; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }

        [JsonProperty("warehouse_quantity_free")]
        public int WarehouseQuantityFree { get; set; }

        [JsonProperty("sales_quantity")]
        public int SalesQuantity { get; init; }

        [JsonProperty("total_sales_quantity")]
        public int TotalSalesQuantity { get; init; }

        [JsonProperty("sales_quantity_by_warehouse")]
        public int SalesQuantityByWarehouse { get; init; }

        [JsonProperty("min_leftover")]
        public int? MinLeftover { get; init; }

        [JsonProperty("can_buy")]
        public bool CanBuy { get; set; }

        [JsonProperty("warehouse_category_plan")]
        public int WarehouseCategoryPlan { get; set; }

        [JsonProperty("category_employee_id")]
        public int CategoryEmployeeId { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonIgnore]
        public int SumCapacity { get; private set; }

        public void SetSumCapacity (int sumCapacity)
        {
            SumCapacity = sumCapacity;
        }
    }
}