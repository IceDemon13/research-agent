using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ModuleHelpUrlDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("module_name")]
        public string ModuleName { get; init; }

        [JsonProperty("module_view")]
        public string ModuleView { get; init; }

        [JsonProperty("telewiki_url")]
        public string TelewikiUrl { get; init; }
    }
}