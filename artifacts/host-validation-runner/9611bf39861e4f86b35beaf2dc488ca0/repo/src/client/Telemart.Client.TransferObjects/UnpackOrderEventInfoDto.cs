using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record UnpackOrderEventInfoDto
    {
        [JsonProperty("event_for_warehouse_employees")]
        public bool EventForWarehouseEmployees { get; init; }

        [JsonProperty("task_for_pickup_employees")]
        public bool TaskForPickupEmployees { get; init; }

        [JsonProperty("errors")]
        public IReadOnlyCollection<string> Errors { get; init; }
    }
}