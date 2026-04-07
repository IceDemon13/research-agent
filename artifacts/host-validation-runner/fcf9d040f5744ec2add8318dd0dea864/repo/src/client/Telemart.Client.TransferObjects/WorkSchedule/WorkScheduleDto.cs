using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkSchedule
{
    public sealed record WorkScheduleDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("start")]
        public TimeSpan Start { get; init; }

        [JsonProperty("end")]
        public TimeSpan End { get; init; }

        [JsonProperty("days")]
        public string Days { get; init; }
    }
}