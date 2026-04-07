using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public sealed record AddToPlanBacklogTaskDto
    {
        [JsonProperty("estimate")]
        public int Estimate { get; init; }

        [JsonProperty("quota_id")]
        public int QuotaId { get; init; }
    }
}