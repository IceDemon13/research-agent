using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderGeneralConfirmSettingDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("display_name")]
        public string DisplayName { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }
    }
}