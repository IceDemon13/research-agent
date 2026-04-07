using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record SaveOrderGeneralConfirmSettingDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }
    }
}