using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public class ParserSettingsCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parser_id")]
        public int ParserId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("comparison_type")]
        public int UrlComparisonType { get; set; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("brand_ignore")]
        public bool BrandIgnore { get; set; }

        [JsonProperty("categories")]
        public List<int> Categories { get; set; }

        [JsonProperty("ok_words")]
        public List<string> OkWords { get; set; }

        [JsonProperty("stop_words")]
        public List<string> StopWords { get; set; }

        [JsonProperty("replaces")]
        public List<ParserSettingsCategoryReplaceDto> Replaces { get; set; }
    }
}