using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public sealed record ParserSettingsFileColumnDto
    {
        [JsonProperty("name_column_numbers")]
        public string NameColumnNumbers { get; init; }

        [JsonProperty("category_column_numbers")]
        public string CategoryColumnNumbers { get; init; }

        [JsonProperty("code_column_numbers")]
        public string CodeColumnNumbers { get; init; }

        [JsonProperty("pn_column_numbers")]
        public string PartNumberColumnNumbers { get; init; }

        [JsonProperty("avail_column_number")]
        public byte? AvailColumnNumber { get; init; }

        [JsonProperty("category_rule1")]
        public ParserSettingsFileColumnCategoryRuleDto CategoryRule1 { get; init; }

        [JsonProperty("category_rule2")]
        public ParserSettingsFileColumnCategoryRuleDto CategoryRule2 { get; init; }

        [JsonProperty("category_rule3")]
        public ParserSettingsFileColumnCategoryRuleDto CategoryRule3 { get; init; }

        [JsonProperty("prices")]
        public IReadOnlyCollection<ParserSettingsFilePriceDto> Prices { get; init; }
    }
}