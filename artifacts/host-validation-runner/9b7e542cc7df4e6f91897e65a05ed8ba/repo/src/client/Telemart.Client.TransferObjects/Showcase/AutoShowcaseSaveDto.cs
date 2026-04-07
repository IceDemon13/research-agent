using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record AutoShowcaseSaveDto
    {
        public AutoShowcaseSaveDto(int id, int warehouseId, int productId, int capacity, int modifiedBy, int createdBy)
        {
            Id = id;
            WarehouseId = warehouseId;
            ProductId = productId;
            Capacity = capacity;
            ModifiedBy = modifiedBy;
            CreatedBy = createdBy;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("capacity")]
        public int Capacity { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}