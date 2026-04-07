using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkSchedule
{
    public sealed record WorkScheduleTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}