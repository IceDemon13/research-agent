using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Perfomance
{
    public sealed record DeletePerfomancesDto
    {
        [JsonProperty("performance_ids")]
        public IReadOnlyCollection<int> PerformancesIds { get; init; }
    }
}