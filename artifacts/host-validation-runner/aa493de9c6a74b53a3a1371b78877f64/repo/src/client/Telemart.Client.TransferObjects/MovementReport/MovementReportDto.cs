using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MovementReport
{
    public class MovementReportDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("warehouse_from_name")]
        public string WarehouseFromName { get; init; }

        [JsonProperty("warehouse_to_name")]
        public string WarehouseToName { get; init; }

        [JsonProperty("groups")]
        public IReadOnlyCollection<MovementReportGroupDto> Groups { get; init; }
    }
}