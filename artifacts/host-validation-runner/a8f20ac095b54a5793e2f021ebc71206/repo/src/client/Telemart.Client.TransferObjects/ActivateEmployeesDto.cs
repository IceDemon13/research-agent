using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ActivateEmployeesDto
    {
        [JsonProperty("work_place_type_ids")]
        public IReadOnlyCollection<int> WorkPlaceTypeIds { get; init; }
    }
}