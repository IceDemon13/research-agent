using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Quotas
{
    public sealed record QuotaSaveDto
    {
        [JsonProperty("quota_value")]
        public int QuotaValue { get; init; }
    }
}