using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MovementReport
{
    public class MovementReportGroupDto
    {
        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<MovementReportProductDto> Products { get; init; }
    }
}