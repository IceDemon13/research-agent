using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Quotas
{
    public sealed record MaxQuotaIt
    {
        [JsonProperty("value")]
        public int Value { get; init; }
    }
}