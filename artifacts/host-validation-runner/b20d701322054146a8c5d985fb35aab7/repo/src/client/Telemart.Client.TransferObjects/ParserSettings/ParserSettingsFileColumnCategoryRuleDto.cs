using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public sealed record ParserSettingsFileColumnCategoryRuleDto
    {
        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("column_number")]
        public byte ColumnNumber { get; init; }

        [JsonProperty("equals_condition")]
        public bool EqualsCondition { get; init; }

        [JsonProperty("pattern")]
        public string Pattern { get; init; }
    }
}