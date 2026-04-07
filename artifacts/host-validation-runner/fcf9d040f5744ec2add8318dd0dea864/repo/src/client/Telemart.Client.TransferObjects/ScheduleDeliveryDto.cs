using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ScheduleDeliveryDto
    {
        [JsonProperty("orders")]
        public IReadOnlyCollection<ScheduleDeliveryOrderDto> Orders { get; init; }
    }
}