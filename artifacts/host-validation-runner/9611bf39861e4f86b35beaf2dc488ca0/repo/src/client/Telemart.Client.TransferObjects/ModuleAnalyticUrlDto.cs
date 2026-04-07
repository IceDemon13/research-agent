using System;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects
{
    public class ModuleAnalyticUrlDto : TrackableDtoBase<int>
    {
        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("width")]
        public int Width { get; init; }

        [JsonProperty("module_view")]
        public string ModuleView { get; init; }

        [JsonProperty("url")]
        public string Url { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }
    }
}