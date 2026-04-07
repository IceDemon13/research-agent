using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse.Cell;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class UpdateWarehouseCell : UpdateEntityResultRequestBase<WarehouseCellDto, UpdateWarehouseCell.WarehouseCellUpdateDto>
    {
        public UpdateWarehouseCell(int id, int warehouseId, string name, bool active)
            : base(new WarehouseCellUpdateDto(id, warehouseId, name, active), ApiResources.Warehouses, warehouseId, ApiResources.Cells, id)
        {
        }

        public class WarehouseCellUpdateDto
        {
            public WarehouseCellUpdateDto(int id, int warehouseId, string name, bool active)
            {
                Id = id;
                WarehouseId = warehouseId;
                Name = name;
                Active = active;
            }

            [JsonProperty("id")]
            public int Id { get; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; }

            [JsonProperty("name")]
            public string Name { get; }

            [JsonProperty("active")]
            public bool Active { get; }
        }
    }
}