using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record AutoShowcasesSaveDto
    {
        public AutoShowcasesSaveDto(
            IReadOnlyCollection<int> deletedIds,
            IReadOnlyCollection<AutoShowcaseSaveDto> showcases,
            IReadOnlyCollection<int> selectedWarehouseIds)
        {
            Showcases = showcases;
            DeletedIds = deletedIds;
            SelectedWarehouseIds = selectedWarehouseIds;
        }

        [JsonProperty("deleted_ids")]
        public IReadOnlyCollection<int> DeletedIds { get; init; }

        [JsonProperty("showcases")]
        public IReadOnlyCollection<AutoShowcaseSaveDto> Showcases { get; init; }

        [JsonProperty("selected_warehouse_ids")]
        public IReadOnlyCollection<int> SelectedWarehouseIds { get; init; }
    }
}