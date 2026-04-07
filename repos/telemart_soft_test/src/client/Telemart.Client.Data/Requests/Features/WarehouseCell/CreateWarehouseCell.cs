using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class CreateWarehouseCell : CreateEntityResultRequestBase<WarehouseCellDto, CreateWarehouseCell.WarehouseCellCreateDto>
    {
        public CreateWarehouseCell(int warehouseId, string name, bool active)
            : base(new WarehouseCellCreateDto(warehouseId, name, active), ApiResources.Warehouses, warehouseId, ApiResources.Cells)
        {
        }

        public class WarehouseCellCreateDto
        {
            public WarehouseCellCreateDto(int warehouseId, string name, bool active)
            {
                WarehouseId = warehouseId;
                Name = name;
                Active = active;
            }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("active")]
            public bool Active { get; set; }
        }
    }
}