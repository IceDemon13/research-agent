using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderLogisticsDto
    {
        public OrderLogisticsDto(
            int id,
            int warehouseId,
            int? assemblyWarehouseId,
            int? bufferWarehouseId,
            int? additionalServiceWarehouseId,
            int carryId,
            int subdivisionId,
            IReadOnlyCollection<OrderProductLogisticsDto> orderProducts)
        {
            Id = id;
            WarehouseId = warehouseId;
            AssemblyWarehouseId = assemblyWarehouseId;
            BufferWarehouseId = bufferWarehouseId;
            AdditionalServiceWarehouseId = additionalServiceWarehouseId;
            CarryId = carryId;
            SubdivisionId = subdivisionId;
            OrderProducts = orderProducts;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("assembly_warehouse_id")]
        public int? AssemblyWarehouseId { get; set; }

        [JsonProperty("buffer_warehouse_id")]
        public int? BufferWarehouseId { get; set; }

        [JsonProperty("additional_service_warehouse_id")]
        public int? AdditionalServiceWarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<OrderProductLogisticsDto> OrderProducts { get; set; }
    }
}