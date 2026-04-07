using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.RobotProperty
{
    public sealed record RobotPropertyDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("display_name")]
        public string DisplayName { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("position")]
        public int Position { get; init; }

        [JsonProperty("group_position")]
        public int GroupPosition { get; init; }

        [JsonProperty("regex")]
        public string Regex { get; init; }

        [JsonProperty("group_name")]
        public string GroupName { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("required")]
        public bool Required { get; init; }

        [JsonProperty("values")]
        public IReadOnlyCollection<RobotPropertyValueDto> Values { get; init; }
    }
}