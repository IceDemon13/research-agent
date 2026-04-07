using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Perfomance
{
    public sealed record WarehouseManyPerfomancesSaveDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("perfomances")]
        public IReadOnlyCollection<WarehousePerfomanceSaveDto> Perfomances { get; init; }
    }
}