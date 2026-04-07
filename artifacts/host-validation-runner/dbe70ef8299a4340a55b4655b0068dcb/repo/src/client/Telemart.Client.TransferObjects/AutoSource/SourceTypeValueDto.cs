using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class SourceTypeValueDto
    {
        public SourceTypeValueDto(string sourceType, bool value, int position, string name, string description)
        {
            SourceType = sourceType;
            Name = name;
            Value = value;
            Position = position;
            Description = description;
        }

        public SourceTypeValueDto()
        {
        }

        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        [JsonProperty("value")]
        public bool Value { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}