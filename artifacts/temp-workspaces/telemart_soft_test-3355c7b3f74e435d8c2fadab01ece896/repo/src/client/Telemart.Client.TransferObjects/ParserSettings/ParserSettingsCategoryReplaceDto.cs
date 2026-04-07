using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public class ParserSettingsCategoryReplaceDto
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }
}