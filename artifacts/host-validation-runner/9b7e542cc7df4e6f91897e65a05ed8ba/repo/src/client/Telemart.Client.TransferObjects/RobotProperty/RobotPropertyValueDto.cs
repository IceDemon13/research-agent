using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.RobotProperty
{
    public sealed record RobotPropertyValueDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("robot_property_id")]
        public int RobotPropertyId { get; init; }

        [JsonProperty("display_name")]
        public string DisplayName { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }
    }
}