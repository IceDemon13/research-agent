using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public sealed record DeleteRoutesDto
    {
        [JsonProperty("route_ids")]
        public IReadOnlyCollection<int> RouteIds { get; init; }
    }
}